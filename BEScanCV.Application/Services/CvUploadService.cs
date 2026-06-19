using System.IO.Compression;
using BEScanCV.Application.DTOS;
using BEScanCV.Application.Exceptions;
using BEScanCV.Application.Interfaces;
using BEScanCV.Application.Interfaces.Repositories;
using BEScanCV.Domain.Entities;

namespace BEScanCV.Application.Services;

public sealed class CvUploadService(
    ICvFileRepository cvFileRepository,
    ICvFileStorageService cvFileStorageService,
    ICvUploadBatchRepository cvUploadBatchRepository,
    ICvUploadJobQueue cvUploadJobQueue,
    IUploadProgressNotifier uploadProgressNotifier) : ICvUploadService
{
    private static readonly string[] AllowedExtensions = [".pdf", ".docx", ".doc"];
    private const int MaxFilesPerRequest = 5;
    private const long MaxFileSizeBytes = 20 * 1024 * 1024;
    private const long DefaultUploadedBy = 1;

    public async Task<CvBulkUploadResponse> BulkUploadAsync(
        CvBulkUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var existingBatch = await cvUploadBatchRepository.GetByRequestIdAsync(
            request.RequestId.Trim(),
            cancellationToken);

        if (existingBatch is not null)
        {
            return new CvBulkUploadResponse
            {
                BatchId = existingBatch.Id,
                AcceptedFiles = 0,
                TotalAcceptedFiles = existingBatch.TotalFiles,
                WebsocketEndpoint = $"/ws/upload-progress/{existingBatch.Id}"
            };
        }

        var batchId = string.IsNullOrWhiteSpace(request.BatchId)
            ? $"batch_{Guid.NewGuid():N}"
            : request.BatchId.Trim();

        var acceptedFiles = 0;
        var uploadedBy = request.UploadedBy.GetValueOrDefault(DefaultUploadedBy);
        var validatedFiles = new List<ValidatedUploadFile>();

        foreach (var file in request.Files)
        {
            var originalFileName = NormalizeOriginalFileName(file.FileName);
            var content = await ReadFileAsync(file, cancellationToken);
            var fileType = ValidateFile(originalFileName, content);
            validatedFiles.Add(new ValidatedUploadFile(originalFileName, fileType, content));
        }

        var batch = await EnsureBatchAsync(batchId, request.RequestId.Trim(), uploadedBy, cancellationToken);

        foreach (var file in validatedFiles)
        {
            var fileUrl = await cvFileStorageService.SaveAsync(
                file.OriginalFileName,
                file.Content,
                cancellationToken);

            var now = DateTime.UtcNow;
            var cvFile = new CvFile
            {
                UploadedBy = uploadedBy,
                OriginalFileName = file.OriginalFileName,
                FileUrl = fileUrl,
                FileType = file.FileType,
                FileSize = file.Content.LongLength,
                CreatedAt = now,
                UpdatedAt = now
            };

            await cvFileRepository.AddAsync(cvFile, cancellationToken);

            batch.TotalFiles++;
            batch.PendingFiles++;
            batch.UpdatedAt = DateTime.UtcNow;
            await cvUploadBatchRepository.UpdateBatchAsync(batch, cancellationToken);

            await cvUploadJobQueue.EnqueueAsync(
                new CvUploadJob(
                    batch.Id,
                    request.RequestId.Trim(),
                    cvFile.Id,
                    file.OriginalFileName,
                    file.FileType,
                    fileUrl),
                cancellationToken);

            acceptedFiles++;
        }

        await NotifyBatchProgressAsync(batch.Id, cancellationToken);

        return new CvBulkUploadResponse
        {
            BatchId = batch.Id,
            AcceptedFiles = acceptedFiles,
            TotalAcceptedFiles = batch.TotalFiles,
            WebsocketEndpoint = $"/ws/upload-progress/{batch.Id}"
        };
    }

    public Task<CvBatchUploadStatusResponse?> GetBatchStatusAsync(
        string batchId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(batchId))
        {
            throw new CvUploadValidationException("batchId is required.");
        }

        return cvUploadBatchRepository.GetStatusAsync(batchId.Trim(), cancellationToken);
    }

    public async Task<CvBatchCancelResponse> CancelBatchAsync(
        string batchId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(batchId))
        {
            throw new CvUploadValidationException("batchId is required.");
        }

        var batch = await cvUploadBatchRepository.GetByIdAsync(batchId.Trim(), cancellationToken: cancellationToken);
        if (batch is null)
        {
            throw new CvUploadValidationException("Batch not found.", 404);
        }

        if (batch.Status is "COMPLETED" or "CANCELLED")
        {
            throw new CvUploadValidationException("Batch cannot be cancelled.", 409);
        }

        var status = await cvUploadBatchRepository.RequestCancellationAsync(
            batch.Id,
            cancellationToken);

        if (status is null)
        {
            throw new CvUploadValidationException("Batch cannot be cancelled.", 409);
        }

        if (status.Status == "CANCELLING")
        {
            await uploadProgressNotifier.NotifyAsync(batch.Id, new
            {
                type = "BATCH_CANCELLING",
                batchId = batch.Id
            }, cancellationToken);
        }
        else
        {
            await uploadProgressNotifier.NotifyAsync(batch.Id, new
            {
                type = "BATCH_CANCELLED",
                batchId = batch.Id,
                completedFiles = status.CompletedFiles,
                failedFiles = status.FailedFiles,
                cancelledFiles = status.CancelledFiles
            }, cancellationToken);
        }

        await NotifyBatchProgressAsync(batch.Id, cancellationToken);

        return new CvBatchCancelResponse
        {
            BatchId = batch.Id,
            Status = status.Status
        };
    }

    private async Task<CvUploadBatch> EnsureBatchAsync(
        string batchId,
        string requestId,
        long uploadedBy,
        CancellationToken cancellationToken)
    {
        var batch = await cvUploadBatchRepository.GetByIdAsync(batchId, cancellationToken: cancellationToken);
        if (batch is not null)
        {
            if (batch.Status is "COMPLETED" or "CANCELLED" or "CANCELLING")
            {
                throw new CvUploadValidationException("Batch cannot accept more files.", 409);
            }

            var requestToken = BuildRequestToken(requestId);
            if (!batch.RequestIds.Contains(requestToken, StringComparison.Ordinal))
            {
                batch.RequestIds += requestToken;
                batch.UpdatedAt = DateTime.UtcNow;
                await cvUploadBatchRepository.UpdateBatchAsync(batch, cancellationToken);
            }

            return batch;
        }

        batch = new CvUploadBatch
        {
            Id = batchId,
            UploadedBy = uploadedBy,
            Status = "PENDING",
            RequestIds = BuildRequestToken(requestId),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await cvUploadBatchRepository.AddAsync(batch, cancellationToken);
        return batch;
    }

    private static string BuildRequestToken(string requestId) => $"|{requestId.Trim()}|";

    private async Task<CvBatchUploadStatusResponse?> NotifyBatchProgressAsync(
        string batchId,
        CancellationToken cancellationToken)
    {
        var status = await cvUploadBatchRepository.GetStatusAsync(batchId, cancellationToken);
        if (status is null)
        {
            return null;
        }

        await uploadProgressNotifier.NotifyAsync(batchId, new
        {
            type = "BATCH_PROGRESS",
            batchId,
            totalFiles = status.TotalFiles,
            completedFiles = status.CompletedFiles,
            failedFiles = status.FailedFiles,
            cancelledFiles = status.CancelledFiles,
            processingFiles = status.ProcessingFiles,
            pendingFiles = status.PendingFiles,
            progress = status.Progress
        }, cancellationToken);

        return status;
    }

    private static void ValidateRequest(CvBulkUploadRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RequestId))
        {
            throw new CvUploadValidationException("requestId is required.");
        }

        if (request.Files.Count == 0)
        {
            throw new CvUploadValidationException("At least one file is required.");
        }

        if (request.Files.Count > MaxFilesPerRequest)
        {
            throw new CvUploadValidationException($"A chunk can contain at most {MaxFilesPerRequest} files.");
        }
    }

    private static async Task<byte[]> ReadFileAsync(
        CvBulkUploadFileInput file,
        CancellationToken cancellationToken)
    {
        if (file.Length <= 0)
        {
            throw new CvUploadValidationException($"{file.FileName} is empty.");
        }

        if (file.Length > MaxFileSizeBytes)
        {
            throw new CvUploadValidationException(
                $"{file.FileName} exceeds the maximum allowed size.",
                413);
        }

        await using var stream = file.OpenReadStream();
        await using var memoryStream = new MemoryStream(capacity: (int)Math.Min(file.Length, MaxFileSizeBytes));
        await stream.CopyToAsync(memoryStream, cancellationToken);

        return memoryStream.ToArray();
    }

    private static string ValidateFile(string fileName, byte[] content)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new CvUploadValidationException($"{fileName} must be a PDF, DOCX, or DOC file.");
        }

        if (extension == ".docx" && !IsDocx(content))
        {
            throw new CvUploadValidationException($"{fileName} is not a valid DOCX file.");
        }

        if (extension == ".doc" && !IsDoc(content))
        {
            throw new CvUploadValidationException($"{fileName} is not a valid DOC file.");
        }

        return extension.TrimStart('.');
    }

    private static string NormalizeOriginalFileName(string fileName)
    {
        var normalized = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new CvUploadValidationException("File name is invalid.");
        }

        return normalized;
    }

    private static bool IsDoc(byte[] content)
    {
        byte[] oleHeader = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];
        return content.Length >= oleHeader.Length && content.Take(oleHeader.Length).SequenceEqual(oleHeader);
    }

    private static bool IsDocx(byte[] content)
    {
        try
        {
            using var memoryStream = new MemoryStream(content);
            using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read);
            return archive.GetEntry("[Content_Types].xml") is not null &&
                   archive.GetEntry("word/document.xml") is not null;
        }
        catch (InvalidDataException)
        {
            return false;
        }
    }

    private sealed record ValidatedUploadFile(
        string OriginalFileName,
        string FileType,
        byte[] Content);
}
