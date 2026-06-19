namespace BEScanCV.Application.Interfaces;

public interface ICvUploadJobQueue
{
    ValueTask EnqueueAsync(CvUploadJob job, CancellationToken cancellationToken = default);
    ValueTask<CvUploadJob> DequeueAsync(CancellationToken cancellationToken = default);
    ValueTask AcknowledgeAsync(CvUploadJob job, CancellationToken cancellationToken = default);
    ValueTask RequeueAsync(CvUploadJob job, CancellationToken cancellationToken = default);
    ValueTask<int> RecoverProcessingJobsAsync(CancellationToken cancellationToken = default);
}

public sealed record CvUploadJob(
    string BatchId,
    string RequestId,
    long CvFileId,
    string FileName,
    string FileType,
    string FileUrl);
