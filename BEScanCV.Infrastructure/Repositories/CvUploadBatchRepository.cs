using BEScanCV.Application.DTOS.Response;
using BEScanCV.Application.Interfaces.Repositories;
using BEScanCV.Domain.Entities;
using BEScanCV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BEScanCV.Infrastructure.Repositories;

public sealed class CvUploadBatchRepository(BEScanCvDbContext dbContext, ILogger<CvUploadBatchRepository> logger) : ICvUploadBatchRepository
{
    public Task<CvUploadBatch?> GetByIdAsync(
        string batchId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.CvUploadBatches
            .FirstOrDefaultAsync(batch => batch.Id == batchId, cancellationToken);
    }

    public Task<CvUploadBatch?> GetByRequestIdAsync(
        string requestId,
        CancellationToken cancellationToken = default)
    {
        var requestToken = BuildRequestToken(requestId);
        return dbContext.CvUploadBatches
            .AsNoTracking()
            .FirstOrDefaultAsync(
                batch => batch.RequestIds.Contains(requestToken),
                cancellationToken);
    }

    public async Task AddAsync(CvUploadBatch batch, CancellationToken cancellationToken = default)
    {
        NormalizeDateTimes(batch);
        await dbContext.CvUploadBatches.AddAsync(batch, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Added batch. BatchId: {BatchId} at {Timestamp}", batch.Id, DateTime.UtcNow);
    }

    public async Task UpdateBatchAsync(CvUploadBatch batch, CancellationToken cancellationToken = default)
    {
        NormalizeDateTimes(batch);
        dbContext.CvUploadBatches.Update(batch);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Updated batch. BatchId: {BatchId}, Status: {Status} at {Timestamp}", batch.Id, batch.Status, DateTime.UtcNow);
    }

    public async Task<bool> TryStartFileAsync(
        string batchId,
        long batchUploadItemId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var updatedRows = await dbContext.CvUploadBatches
            .Where(batch =>
                batch.Id == batchId &&
                (batch.Status == "PENDING" || batch.Status == "PROCESSING") &&
                batch.PendingFiles > 0)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(batch => batch.Status, "PROCESSING")
                .SetProperty(batch => batch.PendingFiles, batch => batch.PendingFiles - 1)
                .SetProperty(batch => batch.ProcessingFiles, batch => batch.ProcessingFiles + 1)
                .SetProperty(batch => batch.UpdatedAt, DateTime.UtcNow),
                cancellationToken);

        if (updatedRows == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        var updatedItems = await dbContext.CvUploadBatchItems
            .Where(item =>
                item.Id == batchUploadItemId &&
                item.CvUploadBatchId == batchId &&
                item.Status == "QUEUE")
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.Status, "PROCESSING")
                .SetProperty(item => item.ErrorMessage, (string?)null)
                .SetProperty(item => item.UpdatedAt, DateTime.UtcNow),
                cancellationToken);

        if (updatedItems == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Upload item {batchUploadItemId} cannot be moved to PROCESSING.");
        }

        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Started file processing. BatchId: {BatchId}, BatchUploadItemId: {BatchUploadItemId} at {Timestamp}", batchId, batchUploadItemId, DateTime.UtcNow);
        return true;
    }

    public async Task MarkFileCompletedAsync(
        string batchId,
        long batchUploadItemId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var updatedRows = await dbContext.CvUploadBatches
            .Where(batch => batch.Id == batchId && batch.ProcessingFiles > 0)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(batch => batch.ProcessingFiles, batch => batch.ProcessingFiles - 1)
                .SetProperty(batch => batch.CompletedFiles, batch => batch.CompletedFiles + 1)
                .SetProperty(batch => batch.UpdatedAt, DateTime.UtcNow),
                cancellationToken);

        if (updatedRows == 0)
        {
            throw new InvalidOperationException(
                $"Upload batch {batchId} has no processing file to complete.");
        }

        var updatedItems = await dbContext.CvUploadBatchItems
            .Where(item =>
                item.Id == batchUploadItemId &&
                item.CvUploadBatchId == batchId &&
                item.Status == "PROCESSING")
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.Status, "COMPLETED")
                .SetProperty(item => item.ErrorMessage, (string?)null)
                .SetProperty(item => item.UpdatedAt, DateTime.UtcNow),
                cancellationToken);

        if (updatedItems == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Upload item {batchUploadItemId} cannot be moved to COMPLETED.");
        }

        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Marked file completed. BatchId: {BatchId}, BatchUploadItemId: {BatchUploadItemId} at {Timestamp}", batchId, batchUploadItemId, DateTime.UtcNow);
    }

    public async Task<CvBatchUploadStatusResponse?> RequestCancellationAsync(
        string batchId,
        CancellationToken cancellationToken = default)
    {
        var updatedRows = await dbContext.CvUploadBatches
            .Where(batch =>
                batch.Id == batchId &&
                batch.Status != "COMPLETED" &&
                batch.Status != "CANCELLED")
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(
                    batch => batch.Status,
                    batch => batch.ProcessingFiles > 0 ? "CANCELLING" : "CANCELLED")
                .SetProperty(
                    batch => batch.CancelledFiles,
                    batch => batch.CancelledFiles + batch.PendingFiles)
                .SetProperty(batch => batch.PendingFiles, 0)
                .SetProperty(batch => batch.UpdatedAt, DateTime.UtcNow),
                cancellationToken);

        if (updatedRows > 0)
        {
            logger.LogInformation("Requested batch cancellation. BatchId: {BatchId}, UpdatedRows: {UpdatedRows} at {Timestamp}", batchId, updatedRows, DateTime.UtcNow);
        }

        return updatedRows == 0
            ? null
            : await GetStatusAsync(batchId, cancellationToken);
    }

    public async Task<CvBatchUploadStatusResponse?> TryCompleteIfIdleAsync(
        string batchId,
        CancellationToken cancellationToken = default)
    {
        var updatedRows = await dbContext.CvUploadBatches
            .Where(batch =>
                batch.Id == batchId &&
                batch.PendingFiles == 0 &&
                batch.ProcessingFiles == 0 &&
                batch.Status != "COMPLETED" &&
                batch.Status != "CANCELLED")
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(
                    batch => batch.Status,
                    batch => batch.Status == "CANCELLING" ? "CANCELLED" : "COMPLETED")
                .SetProperty(batch => batch.UpdatedAt, DateTime.UtcNow),
                cancellationToken);

        if (updatedRows > 0)
        {
            logger.LogInformation("Completed idle batch. BatchId: {BatchId} at {Timestamp}", batchId, DateTime.UtcNow);
        }

        return updatedRows == 0
            ? null
            : await GetStatusAsync(batchId, cancellationToken);
    }

    public async Task<CvBatchUploadStatusResponse?> GetStatusAsync(
        string batchId,
        CancellationToken cancellationToken = default)
    {
        var batch = await dbContext.CvUploadBatches
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == batchId, cancellationToken);

        if (batch is null)
        {
            return null;
        }

        var items = await dbContext.CvUploadBatchItems
            .AsNoTracking()
            .Where(item => item.CvUploadBatchId == batchId)
            .OrderBy(item => item.Id)
            .Select(item => new CvBatchUploadItemResponse
            {
                Id = item.Id,
                FileName = item.FileName,
                FileSize = item.FileSize,
                Status = item.Status,
                ErrorMessage = item.ErrorMessage,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt
            })
            .ToArrayAsync(cancellationToken);

        var response = BuildStatus(batch);
        response.Items = items;
        logger.LogInformation("Retrieved batch status. BatchId: {BatchId}, Status: {Status} at {Timestamp}", batchId, response.Status, DateTime.UtcNow);
        return response;
    }

    public static string BuildRequestToken(string requestId) => $"|{requestId.Trim()}|";

    private static CvBatchUploadStatusResponse BuildStatus(CvUploadBatch batch)
    {
        var processed = batch.CompletedFiles + batch.FailedFiles + batch.CancelledFiles;
        var progress = batch.TotalFiles == 0 ? 0 : (int)Math.Round(processed * 100m / batch.TotalFiles);

        return new CvBatchUploadStatusResponse
        {
            BatchId = batch.Id,
            Status = batch.Status,
            TotalFiles = batch.TotalFiles,
            CompletedFiles = batch.CompletedFiles,
            FailedFiles = batch.FailedFiles,
            CancelledFiles = batch.CancelledFiles,
            ProcessingFiles = batch.ProcessingFiles,
            PendingFiles = batch.PendingFiles,
            Progress = progress
        };
    }

    private static void NormalizeDateTimes(CvUploadBatch batch)
    {
        batch.CreatedAt = DateTimeUtcNormalizer.Normalize(batch.CreatedAt);
        batch.UpdatedAt = DateTimeUtcNormalizer.Normalize(batch.UpdatedAt);
    }
}
