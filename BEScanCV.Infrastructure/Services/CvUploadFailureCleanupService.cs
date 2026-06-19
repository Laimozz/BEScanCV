using BEScanCV.Application.Interfaces;
using BEScanCV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BEScanCV.Infrastructure.Services;

public sealed class CvUploadFailureCleanupService(
    BEScanCvDbContext dbContext,
    ICvFileStorageService fileStorageService,
    ILogger<CvUploadFailureCleanupService> logger) : ICvUploadFailureCleanupService
{
    public async Task CleanupFailedAsync(
        string batchId,
        long cvFileId,
        string filePath,
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

        await transaction.CommitAsync(cancellationToken);
        await fileStorageService.DeleteAsync(filePath, cancellationToken);

        logger.LogInformation(
            "Cleaned failed CV upload data. BatchId: {BatchId}, CvFileId: {CvFileId}, FilePath: {FilePath}",
            batchId,
            cvFileId,
            filePath);
    }

    public async Task CleanupCancelledAsync(
        long cvFileId,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await DeleteCvDataAsync(cvFileId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await fileStorageService.DeleteAsync(filePath, cancellationToken);

        logger.LogInformation(
            "Cleaned cancelled CV upload data. CvFileId: {CvFileId}, FilePath: {FilePath}",
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

        await dbContext.CvInfos
            .Where(cvInfo => cvInfo.CvFileId == cvFileId)
            .ExecuteDeleteAsync(cancellationToken);

        return await dbContext.CvFiles
            .Where(cvFile => cvFile.Id == cvFileId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
