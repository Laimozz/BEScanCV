namespace BEScanCV.Application.Interfaces;

public interface ICvCleanupService
{
    Task CleanupFailedAsync(
        string batchId,
        long batchUploadItemId,
        long cvFileId,
        string filePath,
        string errorMessage,
        long? deletedBy,
        CancellationToken cancellationToken = default);

    Task CleanupCancelledAsync(
        long batchUploadItemId,
        long cvFileId,
        string filePath,
        string errorMessage,
        long? deletedBy,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        long cvFileId,
        string filePath,
        long? deletedBy,
        CancellationToken cancellationToken = default);
}
