namespace BEScanCV.Application.Interfaces;

public interface ICvUploadFailureCleanupService
{
    Task CleanupFailedAsync(
        string batchId,
        long cvFileId,
        string filePath,
        CancellationToken cancellationToken = default);

    Task CleanupCancelledAsync(
        long cvFileId,
        string filePath,
        CancellationToken cancellationToken = default);
}
