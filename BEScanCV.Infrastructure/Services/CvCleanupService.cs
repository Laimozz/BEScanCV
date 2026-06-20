using BEScanCV.Application.Interfaces;
using BEScanCV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BEScanCV.Infrastructure.Services;

public sealed class CvCleanupService(
    BEScanCvDbContext dbContext,
    ICvFileStorageService fileStorageService,
    ILogger<CvCleanupService> logger) : ICvCleanupService
{
    public async Task CleanupFailedAsync(
        string batchId,
        long batchUploadItemId,
        long cvFileId,
        string filePath,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var batch = await dbContext.CvUploadBatches
            .FirstOrDefaultAsync(item => item.Id == batchId, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Upload batch {batchId} was not found while cleaning a failed job.");

        var deletedCvFiles = await DeleteCvDataAsync(cvFileId, cancellationToken);
        if (deletedCvFiles > 0)
        {
            batch.ProcessingFiles = Math.Max(0, batch.ProcessingFiles - 1);
            batch.FailedFiles++;
            batch.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await MarkItemFailedAsync(batchUploadItemId, errorMessage, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await fileStorageService.DeleteAsync(filePath, cancellationToken);

        logger.LogInformation(
            "Cleaned failed CV upload data. BatchId: {BatchId}, CvFileId: {CvFileId}, FilePath: {FilePath}",
            batchId,
            cvFileId,
            filePath);
    }

    public async Task CleanupCancelledAsync(
        long batchUploadItemId,
        long cvFileId,
        string filePath,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await DeleteCvDataAsync(cvFileId, cancellationToken);
        await MarkItemFailedAsync(batchUploadItemId, errorMessage, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await fileStorageService.DeleteAsync(filePath, cancellationToken);

        logger.LogInformation(
            "Cleaned cancelled CV upload data. CvFileId: {CvFileId}, FilePath: {FilePath}",
            cvFileId,
            filePath);
    }

    public async Task DeleteAsync(
        long cvFileId,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var deletedCvFiles = await DeleteCvDataAsync(cvFileId, cancellationToken);
        if (deletedCvFiles == 0)
        {
            throw new InvalidOperationException($"CV file {cvFileId} was not found.");
        }

        await transaction.CommitAsync(cancellationToken);
        await fileStorageService.DeleteAsync(filePath, cancellationToken);

        logger.LogInformation(
            "Deleted CV data and local file. CvFileId: {CvFileId}, FilePath: {FilePath}",
            cvFileId,
            filePath);
    }

    private async Task<int> DeleteCvDataAsync(
        long cvFileId,
        CancellationToken cancellationToken)
    {
        var cvInfoIds = dbContext.CvInfos
            .Where(cvInfo => cvInfo.CvFileId == cvFileId)
            .Select(cvInfo => cvInfo.Id);

        await dbContext.CvSkills
            .Where(cvSkill => cvInfoIds.Contains(cvSkill.CvInfoId))
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.CvCertifications
            .Where(certification => cvInfoIds.Contains(certification.CvInfoId))
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.CvWorkExperiences
            .Where(experience => cvInfoIds.Contains(experience.CvInfoId))
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.CvInfos
            .Where(cvInfo => cvInfo.CvFileId == cvFileId)
            .ExecuteDeleteAsync(cancellationToken);

        return await dbContext.CvFiles
            .Where(cvFile => cvFile.Id == cvFileId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private Task<int> MarkItemFailedAsync(
        long batchUploadItemId,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        return dbContext.CvUploadBatchItems
            .Where(item =>
                item.Id == batchUploadItemId &&
                item.Status != "COMPLETED")
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.Status, "FAILED")
                .SetProperty(item => item.ErrorMessage, errorMessage)
                .SetProperty(item => item.UpdatedAt, DateTime.UtcNow),
                cancellationToken);
    }
}
