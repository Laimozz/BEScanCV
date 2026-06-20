namespace BEScanCV.Application.Interfaces;

public interface ICvFileStorageService
{
    Task<string> SaveAsync(
        string originalFileName,
        byte[] content,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string filePath,
        CancellationToken cancellationToken = default);
}
