using BEScanCV.Application.Interfaces;
using BEScanCV.Application.Interfaces.Repositories;
using BEScanCV.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BEScanCV.Infrastructure.Services;

public sealed class CvUploadBackgroundWorker(
    ICvUploadJobQueue queue,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<CvUploadBackgroundWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RecoverQueueAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            CvUploadJob? job = null;

            try
            {
                job = await queue.DequeueAsync(stoppingToken);
                await ProcessJobAsync(job, stoppingToken);
                await queue.AcknowledgeAsync(job, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                if (job is not null)
                {
                    await TryRequeueAsync(job);
                }

                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "CV upload queue or job processing failed. CvFileId: {CvFileId}",
                    job?.CvFileId);

                if (job is not null)
                {
                    await TryRequeueAsync(job);
                }

                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }

    private async Task RecoverQueueAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await queue.RecoverProcessingJobsAsync(stoppingToken);
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unable to recover CV upload jobs from Redis. Retrying.");
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }

    private async Task TryRequeueAsync(CvUploadJob job)
    {
        try
        {
            await queue.RequeueAsync(job, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogCritical(
                ex,
                "Unable to requeue CV upload job in Redis. CvFileId: {CvFileId}",
                job.CvFileId);
        }
    }

    private async Task ProcessJobAsync(CvUploadJob job, CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var batchRepository = scope.ServiceProvider.GetRequiredService<ICvUploadBatchRepository>();
        var cvFileRepository = scope.ServiceProvider.GetRequiredService<ICvFileRepository>();
        var cvInfoRepository = scope.ServiceProvider.GetRequiredService<ICvInfoRepository>();
        var cvSkillRepository = scope.ServiceProvider.GetRequiredService<ICvSkillRepository>();
        var cvProcessingClient = scope.ServiceProvider.GetRequiredService<ICvProcessingClient>();
        var fileStorageService = scope.ServiceProvider.GetRequiredService<ICvFileStorageService>();
        var notifier = scope.ServiceProvider.GetRequiredService<IUploadProgressNotifier>();

        var batch = await batchRepository.GetByIdAsync(job.BatchId, cancellationToken);
        var cvFile = await cvFileRepository.GetByIdAsync(job.CvFileId, cancellationToken);
        if (batch is null || cvFile is null)
        {
            await fileStorageService.DeleteAsync(job.FileUrl, cancellationToken);

            logger.LogWarning(
                "Removed orphaned CV upload file because batch or cv file was not found. BatchId: {BatchId}, CvFileId: {CvFileId}",
                job.BatchId,
                job.CvFileId);

            if (batch is not null)
            {
                await CompleteBatchIfNeededAsync(
                    batchRepository,
                    notifier,
                    job.BatchId,
                    cancellationToken);
            }

            return;
        }

        if (!await batchRepository.TryStartFileAsync(job.BatchId, cancellationToken))
        {
            var currentStatus = await batchRepository.GetStatusAsync(job.BatchId, cancellationToken);
            if (currentStatus?.Status is "CANCELLING" or "CANCELLED")
            {
                logger.LogInformation(
                    "Cleaning cancelled CV upload job. BatchId: {BatchId}, CvFileId: {CvFileId}, Status: {Status}",
                    job.BatchId,
                    job.CvFileId,
                    currentStatus.Status);

                await HandleCancelledUploadAsync(job, cancellationToken);
                return;
            }

            throw new InvalidOperationException(
                $"Unable to start CV upload job {job.CvFileId} for batch {job.BatchId}.");
        }

        await notifier.NotifyAsync(job.BatchId, new
        {
            type = "FILE_STARTED",
            batchId = job.BatchId,
            fileId = job.CvFileId,
            fileName = job.FileName,
            status = "PROCESSING"
        }, cancellationToken);
        await NotifyBatchProgressAsync(batchRepository, notifier, job.BatchId, cancellationToken);

        try
        {
            logger.LogInformation(
                "Start processing CV upload job. BatchId: {BatchId}, CvFileId: {CvFileId}, FileName: {FileName}, FileUrl: {FileUrl}",
                job.BatchId,
                job.CvFileId,
                job.FileName,
                job.FileUrl);

            var result = await cvProcessingClient.SubmitAsync(
                job.CvFileId,
                job.RequestId,
                job.BatchId,
                job.FileName,
                job.FileType,
                job.FileUrl,
                await File.ReadAllBytesAsync(job.FileUrl, cancellationToken),
                cancellationToken);

            logger.LogInformation(
                "AI processing completed. BatchId: {BatchId}, CvFileId: {CvFileId}, AiDocumentId: {AiDocumentId}, FullName: {FullName}",
                job.BatchId,
                job.CvFileId,
                result.AiDocumentId,
                result.FullName ?? result.CandidateName);

            if (!string.IsNullOrWhiteSpace(result.AiDocumentId))
            {
                cvFile.AiDocumentId = result.AiDocumentId;
                cvFile.UpdatedAt = DateTime.UtcNow;
                await cvFileRepository.UpdateAsync(cvFile, cancellationToken);
            }

            await UpsertCvInfoAsync(
                cvInfoRepository,
                cvSkillRepository,
                job.CvFileId,
                result,
                cancellationToken);

            logger.LogInformation(
                "CV extracted data saved. BatchId: {BatchId}, CvFileId: {CvFileId}",
                job.BatchId,
                job.CvFileId);

            await batchRepository.MarkFileCompletedAsync(job.BatchId, cancellationToken);

            await notifier.NotifyAsync(job.BatchId, new
            {
                type = "FILE_COMPLETED",
                batchId = job.BatchId,
                fileId = job.CvFileId,
                fileName = job.FileName,
                status = "COMPLETED",
                candidateName = result.FullName ?? result.CandidateName
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "CV upload job failed. BatchId: {BatchId}, CvFileId: {CvFileId}, FileName: {FileName}, FileUrl: {FileUrl}",
                job.BatchId,
                job.CvFileId,
                job.FileName,
                job.FileUrl);

            await HandleFailedUploadAsync(job, cancellationToken);
            return;
        }

        await CompleteBatchIfNeededAsync(batchRepository, notifier, job.BatchId, cancellationToken);
    }

    private async Task HandleCancelledUploadAsync(
        CvUploadJob job,
        CancellationToken cancellationToken)
    {
        using var cancellationScope = serviceScopeFactory.CreateScope();
        var cleanupService =
            cancellationScope.ServiceProvider.GetRequiredService<ICvUploadFailureCleanupService>();

        await cleanupService.CleanupCancelledAsync(
            job.CvFileId,
            job.FileUrl,
            cancellationToken);
    }

    private async Task HandleFailedUploadAsync(
        CvUploadJob job,
        CancellationToken cancellationToken)
    {
        using var failureScope = serviceScopeFactory.CreateScope();
        var cleanupService =
            failureScope.ServiceProvider.GetRequiredService<ICvUploadFailureCleanupService>();
        var batchRepository =
            failureScope.ServiceProvider.GetRequiredService<ICvUploadBatchRepository>();
        var notifier =
            failureScope.ServiceProvider.GetRequiredService<IUploadProgressNotifier>();

        await cleanupService.CleanupFailedAsync(
            job.BatchId,
            job.CvFileId,
            job.FileUrl,
            cancellationToken);

        await notifier.NotifyAsync(job.BatchId, new
        {
            type = "FILE_FAILED",
            batchId = job.BatchId,
            fileId = job.CvFileId,
            fileName = job.FileName,
            status = "FAILED",
            errorCode = "CV_PROCESSING_ERROR",
            errorMessage = "Unable to process the CV file."
        }, cancellationToken);

        await CompleteBatchIfNeededAsync(
            batchRepository,
            notifier,
            job.BatchId,
            cancellationToken);
    }

    private static async Task UpsertCvInfoAsync(
        ICvInfoRepository cvInfoRepository,
        ICvSkillRepository cvSkillRepository,
        long cvFileId,
        Application.DTOS.CvProcessingResult result,
        CancellationToken cancellationToken)
    {
        var existingCvInfo = await cvInfoRepository.GetByCvFileIdAsync(cvFileId, cancellationToken);
        var now = DateTime.UtcNow;

        if (existingCvInfo is null)
        {
            existingCvInfo = new CvInfo
            {
                CvFileId = cvFileId,
                Status = "NOT_FAVORITE",
                CreatedAt = now
            };

            ApplyCvInfoResult(existingCvInfo, result, now);
            await cvInfoRepository.AddAsync(existingCvInfo, cancellationToken);
        }
        else
        {
            ApplyCvInfoResult(existingCvInfo, result, now);
            await cvInfoRepository.UpdateAsync(existingCvInfo, cancellationToken);
            await cvSkillRepository.DeleteByCvInfoIdAsync(existingCvInfo.Id, cancellationToken);
        }

        var cvSkills = result.Skills
            .Where(skill => !string.IsNullOrWhiteSpace(skill))
            .Select(skill => new CvSkill
            {
                CvInfoId = existingCvInfo.Id,
                Name = skill.Trim()
            })
            .ToArray();

        if (cvSkills.Length > 0)
        {
            await cvSkillRepository.AddRangeAsync(cvSkills, cancellationToken);
        }
    }

    private static void ApplyCvInfoResult(
        CvInfo cvInfo,
        Application.DTOS.CvProcessingResult result,
        DateTime now)
    {
        cvInfo.FullName = result.FullName ?? string.Empty;
        cvInfo.Email = result.Email ?? string.Empty;
        cvInfo.Phone = result.Phone;
        cvInfo.DateOfBirth = result.DateOfBirth;
        cvInfo.Position = result.Position;
        cvInfo.TotalExperienceYears = result.TotalExperienceYears;
        cvInfo.Address = result.Address;
        cvInfo.Summary = result.Summary;
        cvInfo.RawText = result.RawText;
        cvInfo.Educations = result.Educations;
        cvInfo.ProfileData = result.ProfileData;
        cvInfo.UpdatedAt = now;
    }

    private static async Task CompleteBatchIfNeededAsync(
        ICvUploadBatchRepository batchRepository,
        IUploadProgressNotifier notifier,
        string batchId,
        CancellationToken cancellationToken)
    {
        var status = await NotifyBatchProgressAsync(batchRepository, notifier, batchId, cancellationToken);
        if (status is null || status.PendingFiles > 0 || status.ProcessingFiles > 0)
        {
            return;
        }

        var completedStatus = await batchRepository.TryCompleteIfIdleAsync(batchId, cancellationToken);
        if (completedStatus is null)
        {
            return;
        }

        await notifier.NotifyAsync(batchId, new
        {
            type = completedStatus.Status == "CANCELLED" ? "BATCH_CANCELLED" : "BATCH_COMPLETED",
            batchId,
            totalFiles = completedStatus.TotalFiles,
            completedFiles = completedStatus.CompletedFiles,
            failedFiles = completedStatus.FailedFiles,
            cancelledFiles = completedStatus.CancelledFiles
        }, cancellationToken);
    }

    private static async Task<Application.DTOS.CvBatchUploadStatusResponse?> NotifyBatchProgressAsync(
        ICvUploadBatchRepository batchRepository,
        IUploadProgressNotifier notifier,
        string batchId,
        CancellationToken cancellationToken)
    {
        var status = await batchRepository.GetStatusAsync(batchId, cancellationToken);
        if (status is null)
        {
            return null;
        }

        await notifier.NotifyAsync(batchId, new
        {
            type = "BATCH_PROGRESS",
            batchId,
            totalFiles = status.TotalFiles,
            completedFiles = status.CompletedFiles,
            failedFiles = status.FailedFiles,
            cancelledFiles = status.CancelledFiles,
            processingFiles = status.ProcessingFiles,
            pendingFiles = status.PendingFiles,
            progress = status.Progress
        }, cancellationToken);

        return status;
    }
}
