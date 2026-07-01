using BEScanCV.Application.Interfaces.Repositories;
using BEScanCV.Domain.Entities;
using BEScanCV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BEScanCV.Infrastructure.Repositories;

public sealed class CvUploadBatchItemRepository(BEScanCvDbContext dbContext, ILogger<CvUploadBatchItemRepository> logger)
    : ICvUploadBatchItemRepository
{
    public Task<CvUploadBatchItem?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        return dbContext.CvUploadBatchItems
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public async Task AddQueuedAsync(
        CvUploadBatchItem item,
        CancellationToken cancellationToken = default)
    {
        item.CreatedAt = DateTimeUtcNormalizer.Normalize(item.CreatedAt);
        item.UpdatedAt = DateTimeUtcNormalizer.Normalize(item.UpdatedAt);

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var updatedBatches = await dbContext.CvUploadBatches
            .Where(batch =>
                batch.Id == item.CvUploadBatchId &&
                (batch.Status == "PENDING" || batch.Status == "PROCESSING"))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(batch => batch.TotalFiles, batch => batch.TotalFiles + 1)
                .SetProperty(batch => batch.PendingFiles, batch => batch.PendingFiles + 1)
                .SetProperty(batch => batch.UpdatedAt, DateTime.UtcNow),
                cancellationToken);

        if (updatedBatches == 0)
        {
            throw new InvalidOperationException(
                $"Upload batch {item.CvUploadBatchId} cannot accept another file.");
        }

        await dbContext.CvUploadBatchItems.AddAsync(item, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("Added queued batch item. BatchId: {BatchId}, FileName: {FileName} at {Timestamp}", item.CvUploadBatchId, item.FileName, DateTime.UtcNow);
    }
}
