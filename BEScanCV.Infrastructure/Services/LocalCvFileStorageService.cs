using BEScanCV.Application.Interfaces;

namespace BEScanCV.Infrastructure.Services;

public sealed class LocalCvFileStorageService : ICvFileStorageService
{
    private const string LocalPdfFolder = @"D:\PDFLocal";

    public async Task<string> SaveAsync(
        string originalFileName,
        byte[] content,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(LocalPdfFolder);

        var extension = Path.GetExtension(originalFileName);
        var name = Path.GetFileNameWithoutExtension(originalFileName);
        var safeName = string.Concat(name.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '-' : ch));

        if (string.IsNullOrWhiteSpace(safeName))
        {
            safeName = "cv";
        }

        var storedFileName = $"{Guid.NewGuid():N}_{safeName}{extension}";
        var filePath = Path.Combine(LocalPdfFolder, storedFileName);

        await File.WriteAllBytesAsync(filePath, content, cancellationToken);

        return filePath;
    }

    public Task DeleteAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(filePath))
        {
            return Task.CompletedTask;
        }

        var storageRoot = Path.GetFullPath(LocalPdfFolder)
            .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(filePath);

        if (!fullPath.StartsWith(storageRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The file is outside the configured CV storage folder.");
        }

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }
}
