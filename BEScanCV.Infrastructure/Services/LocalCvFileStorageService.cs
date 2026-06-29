using BEScanCV.Application.Interfaces;
using BEScanCV.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace BEScanCV.Infrastructure.Services;

public sealed class LocalCvFileStorageService : ICvFileStorageService
{
    private readonly string _localPdfFolder;

    public LocalCvFileStorageService(IOptions<CvStorageOptions> options)
    {
        _localPdfFolder = string.IsNullOrWhiteSpace(options.Value.LocalPdfFolder)
            ? @"D:\PDFLocal"
            : options.Value.LocalPdfFolder;
    }

    public async Task<string> SaveAsync(
        string originalFileName,
        byte[] content,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_localPdfFolder);

        var extension = Path.GetExtension(originalFileName);
        var name = Path.GetFileNameWithoutExtension(originalFileName);
        var safeName = string.Concat(name.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '-' : ch));

        if (string.IsNullOrWhiteSpace(safeName))
        {
            safeName = "cv";
        }

        var storedFileName = $"{Guid.NewGuid():N}_{safeName}{extension}";
        var filePath = Path.Combine(_localPdfFolder, storedFileName);

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

        var storageRoot = Path.GetFullPath(_localPdfFolder)
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
