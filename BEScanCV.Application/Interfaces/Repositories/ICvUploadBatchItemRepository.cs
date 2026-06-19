using BEScanCV.Domain.Entities;

namespace BEScanCV.Application.Interfaces.Repositories;

public interface ICvUploadBatchItemRepository
{
    Task<CvUploadBatchItem?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default);

    Task AddQueuedAsync(
        CvUploadBatchItem item,
        CancellationToken cancellationToken = default);
}
