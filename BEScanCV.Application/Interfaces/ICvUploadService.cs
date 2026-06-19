using BEScanCV.Application.DTOS;

namespace BEScanCV.Application.Interfaces;

public interface ICvUploadService
{
    Task<CvBulkUploadResponse> BulkUploadAsync(
        CvBulkUploadRequest request,
        CancellationToken cancellationToken = default);

    Task<CvBatchUploadStatusResponse?> GetBatchStatusAsync(
        string batchId,
        CancellationToken cancellationToken = default);

    Task<CvBatchCancelResponse> CancelBatchAsync(
        string batchId,
        CancellationToken cancellationToken = default);
}

