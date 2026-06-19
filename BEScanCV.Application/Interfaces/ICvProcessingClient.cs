using BEScanCV.Application.DTOS;

namespace BEScanCV.Application.Interfaces;

public interface ICvProcessingClient
{
    Task<CvProcessingResult> SubmitAsync(
        long cvFileId,
        string requestId,
        string batchId,
        string originalFileName,
        string fileType,
        string fileUrl,
        byte[] content,
        CancellationToken cancellationToken = default);
}
