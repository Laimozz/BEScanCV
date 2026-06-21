using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Nodes;
using BEScanCV.Application.DTOS;
using BEScanCV.Application.Exceptions;
using BEScanCV.Application.Interfaces;
using BEScanCV.Application.Interfaces.Repositories;
using BEScanCV.Domain.Entities;

namespace BEScanCV.Application.Services;

public sealed class CvService(
    ICvFileRepository cvFileRepository,
    ICvFileStorageService cvFileStorageService,
    ICvInfoRepository cvInfoRepository,
    ICvCleanupService cvCleanupService,
    ICvUploadBatchRepository cvUploadBatchRepository,
    ICvUploadBatchItemRepository cvUploadBatchItemRepository,
    ICvUploadJobQueue cvUploadJobQueue,
    IUploadProgressNotifier uploadProgressNotifier) : ICvService
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

            var batchItem = new CvUploadBatchItem
            {
                CvUploadBatchId = batch.Id,
                FileName = file.OriginalFileName,
                FileSize = file.Content.LongLength,
                Status = "QUEUE",
                CreatedAt = now,
                UpdatedAt = now
            };
            await cvUploadBatchItemRepository.AddQueuedAsync(batchItem, cancellationToken);

            await cvUploadJobQueue.EnqueueAsync(
                new CvUploadJob(
                    batch.Id,
                    batchItem.Id,
                    request.RequestId.Trim(),
                    cvFile.Id,
                    file.OriginalFileName,
                    file.FileType,
                    fileUrl),
                cancellationToken);

            await uploadProgressNotifier.NotifyAsync(batch.Id, new
            {
                type = "FILE_QUEUED",
                batchId = batch.Id,
                itemId = batchItem.Id,
                fileId = cvFile.Id,
                fileName = file.OriginalFileName,
                status = "QUEUE"
            }, cancellationToken);

            acceptedFiles++;
        }

        await NotifyBatchProgressAsync(batch.Id, cancellationToken);
        var batchStatus = await cvUploadBatchRepository.GetStatusAsync(
            batch.Id,
            cancellationToken);

        return new CvBulkUploadResponse
        {
            BatchId = batch.Id,
            AcceptedFiles = acceptedFiles,
            TotalAcceptedFiles = batchStatus?.TotalFiles ?? acceptedFiles,
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

        var batch = await cvUploadBatchRepository.GetByIdAsync(
            batchId.Trim(),
            cancellationToken: cancellationToken);
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

    public async Task<CvUpdateResponse> UpdateAsync(
        long cvFileId,
        CvUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (cvFileId <= 0)
        {
            throw new CvUploadValidationException("cvFileId is invalid.");
        }

        ValidateUpdateRequest(request);

        var cvInfo = await cvInfoRepository.GetByCvFileIdAsync(
            cvFileId,
            cancellationToken);
        if (cvInfo is null)
        {
            throw new CvUploadValidationException("CV not found.", 404);
        }

        cvInfo.FullName = request.FullName.Trim();
        cvInfo.Email = request.Email.Trim();
        cvInfo.Phone = NormalizeOptional(request.Phone);
        cvInfo.Address = NormalizeOptional(request.Address);
        cvInfo.Educations = PatchEducationUniversities(
            cvInfo.Educations,
            request.Educations);
        cvInfo.IsMarked = request.IsMarked;
        cvInfo.Note = NormalizeOptional(request.Note);
        cvInfo.UpdatedAt = DateTime.UtcNow;

        var certifications = (request.Certifications ?? [])
            .Where(certification => !string.IsNullOrWhiteSpace(certification))
            .Select(certification => certification.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var requestedExperience in request.WorkExperience ?? [])
        {
            if (requestedExperience.Id is > 0)
            {
                var existingExperience = cvInfo.WorkExperiences
                    .FirstOrDefault(experience => experience.Id == requestedExperience.Id.Value);

                if (existingExperience is null)
                {
                    throw new CvUploadValidationException(
                        $"work_experience id {requestedExperience.Id.Value} was not found.");
                }

                existingExperience.Company = NormalizeOptional(requestedExperience.Company);
                existingExperience.Position = NormalizeOptional(requestedExperience.Position);
                existingExperience.Duration = NormalizeOptional(requestedExperience.Duration);
                continue;
            }

            cvInfo.WorkExperiences.Add(new CvWorkExperience
            {
                CvInfoId = cvInfo.Id,
                Company = NormalizeOptional(requestedExperience.Company),
                Position = NormalizeOptional(requestedExperience.Position),
                Duration = NormalizeOptional(requestedExperience.Duration)
            });
        }

        await cvInfoRepository.UpdateEditableDataAsync(
            cvInfo,
            certifications,
            cancellationToken);

        return new CvUpdateResponse
        {
            CvInfoId = cvInfo.Id,
            CvFileId = cvInfo.CvFileId,
            FullName = cvInfo.FullName,
            Email = cvInfo.Email,
            Phone = cvInfo.Phone,
            Address = cvInfo.Address,
            Educations = cvInfo.Educations?.RootElement.Clone(),
            Certifications = certifications,
            WorkExperience = cvInfo.WorkExperiences
                .Select(experience => new CvWorkExperienceDto
                {
                    Id = experience.Id,
                    Company = experience.Company,
                    Position = experience.Position,
                    Duration = experience.Duration,
                    Responsibility = experience.Responsibility
                })
                .ToArray(),
            IsMarked = cvInfo.IsMarked,
            Note = cvInfo.Note,
            UpdatedAt = cvInfo.UpdatedAt
        };
    }

    public async Task DeleteAsync(
        long cvFileId,
        CancellationToken cancellationToken = default)
    {
        if (cvFileId <= 0)
        {
            throw new CvUploadValidationException("cvFileId is invalid.");
        }

        var cvInfo = await cvInfoRepository.GetByCvFileIdAsync(
            cvFileId,
            cancellationToken);
        if (cvInfo?.CvFile is null)
        {
            throw new CvUploadValidationException("CV not found.", 404);
        }

        await cvCleanupService.DeleteAsync(
            cvInfo.CvFile.Id,
            cvInfo.CvFile.FileUrl,
            cancellationToken);
    }

    public async Task UpdateQualityScoreAsync(
        CvQualityScoreRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.CvId))
        {
            throw new CvUploadValidationException("cv_id is required.");
        }

        if (request.CvId.Trim().Length > 255)
        {
            throw new CvUploadValidationException("cv_id cannot exceed 255 characters.");
        }

        if (request.QualityScore is null)
        {
            throw new CvUploadValidationException("quality_score is required.");
        }

        if (double.IsNaN(request.QualityScore.Value) ||
            double.IsInfinity(request.QualityScore.Value))
        {
            throw new CvUploadValidationException("quality_score is invalid.");
        }

        if (request.QualityDetails is { ValueKind: not System.Text.Json.JsonValueKind.Object })
        {
            throw new CvUploadValidationException("quality_details must be a JSON object.");
        }

        var cvId = request.CvId.Trim();
        var cvInfo = await cvInfoRepository.GetByAiDocumentIdAsync(
            cvId,
            cancellationToken);
        if (cvInfo is null)
        {
            throw new CvUploadValidationException(
                "CV with the provided cv_id was not found.",
                404);
        }

        cvInfo.QualityScore = request.QualityScore.Value;
        cvInfo.QualityReason = NormalizeOptional(request.QualityReason);
        cvInfo.QualityDetails = request.QualityDetails is null
            ? null
            : System.Text.Json.JsonDocument.Parse(
                request.QualityDetails.Value.GetRawText());
        cvInfo.UpdatedAt = DateTime.UtcNow;

        await cvInfoRepository.UpdateAsync(cvInfo, cancellationToken);
    }

    public async Task<IReadOnlyCollection<CvQualityScoreResultResponse>> GetQualityScoresAsync(
        CvQualityScoresRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.UserId <= 0)
        {
            throw new CvUploadValidationException("user_id is required.");
        }

        if (request.CvIds is null || request.CvIds.Length == 0)
        {
            throw new CvUploadValidationException("cv_ids is required.");
        }

        if (request.CvIds.Any(string.IsNullOrWhiteSpace))
        {
            throw new CvUploadValidationException("cv_ids cannot contain an empty value.");
        }

        var cvIds = request.CvIds
            .Select(cvId => cvId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (cvIds.Any(cvId => cvId.Length > 255))
        {
            throw new CvUploadValidationException(
                "Each cv_id cannot exceed 255 characters.");
        }

        var cvInfos = await cvInfoRepository.GetByAiDocumentIdsAsync(
            cvIds,
            request.UserId,
            cancellationToken);

        var cvInfoByAiDocumentId = cvInfos
            .Where(cvInfo => !string.IsNullOrWhiteSpace(cvInfo.CvFile?.AiDocumentId))
            .GroupBy(
                cvInfo => cvInfo.CvFile!.AiDocumentId!,
                StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.First(),
                StringComparer.Ordinal);

        return cvIds
            .Where(cvInfoByAiDocumentId.ContainsKey)
            .Select(cvId =>
            {
                var cvInfo = cvInfoByAiDocumentId[cvId];
                return new CvQualityScoreResultResponse
                {
                    CvId = cvId,
                    QualityScore = cvInfo.QualityScore,
                    QualityReason = cvInfo.QualityReason
                };
            })
            .ToArray();
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

    private static void ValidateUpdateRequest(CvUpdateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            throw new CvUploadValidationException("full_name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new CvUploadValidationException("email is required.");
        }

        if (request.FullName.Trim().Length > 255)
        {
            throw new CvUploadValidationException("full_name cannot exceed 255 characters.");
        }

        if (request.Email.Trim().Length > 255)
        {
            throw new CvUploadValidationException("email cannot exceed 255 characters.");
        }

        if (request.Phone?.Trim().Length > 50)
        {
            throw new CvUploadValidationException("phone cannot exceed 50 characters.");
        }

        if (request.Address?.Trim().Length > 500)
        {
            throw new CvUploadValidationException("address cannot exceed 500 characters.");
        }

        if ((request.Educations ?? []).Any(
                education => education.University?.Trim().Length > 500))
        {
            throw new CvUploadValidationException(
                "education university cannot exceed 500 characters.");
        }

        if ((request.Certifications ?? []).Any(
                certification => certification?.Trim().Length > 255))
        {
            throw new CvUploadValidationException(
                "Each certification cannot exceed 255 characters.");
        }

        foreach (var experience in request.WorkExperience ?? [])
        {
            if (experience.Id is <= 0)
            {
                throw new CvUploadValidationException(
                    "work_experience id must be greater than zero.");
            }

            if (experience.Company?.Trim().Length > 255 ||
                experience.Position?.Trim().Length > 255 ||
                experience.Duration?.Trim().Length > 255)
            {
                throw new CvUploadValidationException(
                    "work_experience company, position, and duration cannot exceed 255 characters.");
            }
        }
    }

    private static JsonDocument? PatchEducationUniversities(
        JsonDocument? currentEducations,
        IReadOnlyCollection<CvEducationUniversityUpdateRequest>? updates)
    {
        if (updates is null || updates.Count == 0)
        {
            return currentEducations;
        }

        var educationArray = currentEducations?.RootElement.ValueKind == JsonValueKind.Array
            ? JsonNode.Parse(currentEducations.RootElement.GetRawText()) as JsonArray
            : new JsonArray();

        educationArray ??= new JsonArray();
        var updateList = updates.ToArray();

        for (var index = 0; index < updateList.Length; index++)
        {
            JsonObject education;
            if (index < educationArray.Count && educationArray[index] is JsonObject existing)
            {
                education = existing;
            }
            else
            {
                education = new JsonObject();
                educationArray.Add(education);
            }

            education["university"] = NormalizeOptional(updateList[index].University);
        }

        return JsonDocument.Parse(educationArray.ToJsonString());
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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

    private static string GetContentType(string fileType) =>
        fileType.ToLowerInvariant() switch
        {
            "pdf" => "application/pdf",
            "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "doc" => "application/msword",
            _ => "application/octet-stream"
        };

    private sealed record ValidatedUploadFile(
        string OriginalFileName,
        string FileType,
        byte[] Content);
}
