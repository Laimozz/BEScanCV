using BEScanCV.Application.DTOS.Requests;
using BEScanCV.Application.DTOS.Response;

namespace BEScanCV.Application.Interfaces;

public interface ICvService
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

    Task<CvUpdateResponse> UpdateAsync(
        long cvInfoId,
        CvUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        long cvInfoId,
        long? deletedBy,
        CancellationToken cancellationToken = default);

    Task UpdateQualityScoreAsync(
        CvQualityScoreRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CvQualityScoreResultResponse>> GetQualityScoresAsync(
        CvQualityScoresRequest request,
        CancellationToken cancellationToken = default);
}
