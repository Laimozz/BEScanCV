using BEScanCV.Application.DTOS.Response;
using BEScanCV.Domain.Entities;

namespace BEScanCV.Application.Interfaces.Repositories;

public interface ICvUploadBatchRepository
{
    Task<CvUploadBatch?> GetByIdAsync(
        string batchId,
        CancellationToken cancellationToken = default);

    Task<CvUploadBatch?> GetByRequestIdAsync(
        string requestId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        CvUploadBatch batch,
        CancellationToken cancellationToken = default);

    Task UpdateBatchAsync(
        CvUploadBatch batch,
        CancellationToken cancellationToken = default);

    Task<bool> TryStartFileAsync(
        string batchId,
        long batchUploadItemId,
        CancellationToken cancellationToken = default);

    Task MarkFileCompletedAsync(
        string batchId,
        long batchUploadItemId,
        CancellationToken cancellationToken = default);

    Task<CvBatchUploadStatusResponse?> RequestCancellationAsync(
        string batchId,
        CancellationToken cancellationToken = default);

    Task<CvBatchUploadStatusResponse?> TryCompleteIfIdleAsync(
        string batchId,
        CancellationToken cancellationToken = default);

    Task<CvBatchUploadStatusResponse?> GetStatusAsync(
        string batchId,
        CancellationToken cancellationToken = default);

}
