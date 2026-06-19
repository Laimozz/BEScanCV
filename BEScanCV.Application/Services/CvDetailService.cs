using System.Text.Json;
using BEScanCV.Application.DTOS;
using BEScanCV.Application.Interfaces;
using BEScanCV.Application.Interfaces.Repositories;
using BEScanCV.Domain.Entities;

namespace BEScanCV.Application.Services;

public sealed class CvDetailService(ICvInfoRepository cvInfoRepository) : ICvDetailService
{
    private const string LocalPdfFolder = @"D:\PDFLocal";

    public async Task<CvDetailResponse?> GetByCvFileIdAsync(
        long cvFileId,
        string requestBaseUrl,
        CancellationToken cancellationToken = default)
    {
        if (cvFileId <= 0)
            throw new ArgumentException("cv_file_id is invalid.", nameof(cvFileId));

        var cv = await cvInfoRepository.GetByCvFileIdAsync(cvFileId, cancellationToken);
        if (cv is null)
            return null;

        return BuildResponse(cv, requestBaseUrl);
    }

    private static CvDetailResponse BuildResponse(CvInfo cv, string requestBaseUrl)
    {
        var cvFile = cv.CvFile;
        var uploader = cvFile?.Uploader;
        var fileUrl = cvFile?.FileUrl;

        string? pdfUrl = null;

        if (!string.IsNullOrWhiteSpace(fileUrl))
        {
            if (IsRemoteUrl(fileUrl))
            {
                // File trên cloud/remote → dùng URL gốc luôn
                pdfUrl = fileUrl;
            }
            else
            {
                // File local D:\PDFLocal → build public URL qua route /files
                var fileName = Path.GetFileName(
                    fileUrl.Replace('/', Path.DirectorySeparatorChar)
                           .Replace('\\', Path.DirectorySeparatorChar));

                var baseUrl = requestBaseUrl.TrimEnd('/');
                pdfUrl = $"{baseUrl}/files/{fileName}";
            }
        }

        return new CvDetailResponse
        {
            CvInfoId = cv.Id,
            CvFileId = cv.CvFileId,
            FullName = cv.FullName,
            Email = cv.Email,
            Phone = cv.Phone,
            Position = cv.Position,
            TotalExperienceYears = cv.TotalExperienceYears,
            DateOfBirth = cv.DateOfBirth,
            Address = cv.Address,
            Summary = cv.Summary,
            Educations = cv.Educations?.RootElement.Clone(),
            Skills = cv.CvSkills
                .Select(s => s.Name)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            Status = cv.Status,
            OriginalFileName = cvFile?.OriginalFileName ?? string.Empty,
            FileType = cvFile?.FileType ?? string.Empty,
            FileSize = cvFile?.FileSize ?? 0,
            CreatedAt = cv.CreatedAt,
            UpdatedAt = cv.UpdatedAt,
            UploadedBy = new CvUploaderDto(
                cvFile?.UploadedBy ?? 0,
                uploader?.FullName ?? string.Empty),
            PdfUrl = pdfUrl
        };
    }

    private static bool IsRemoteUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
