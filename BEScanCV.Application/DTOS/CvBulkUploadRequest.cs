namespace BEScanCV.Application.DTOS;

public sealed record CvBulkUploadRequest(
    IReadOnlyCollection<CvBulkUploadFileInput> Files,
    string RequestId,
    string? BatchId,
    long? UploadedBy);

public sealed record CvBulkUploadFileInput(
    string FileName,
    string? ContentType,
    long Length,
    Func<Stream> OpenReadStream);

