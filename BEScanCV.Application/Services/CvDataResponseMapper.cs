using BEScanCV.Application.DTOS;
using BEScanCV.Application.DTOS.Response;
using BEScanCV.Domain.Entities;

namespace BEScanCV.Application.Services;

internal static class CvDataResponseMapper
{
    public static TResponse Map<TResponse>(CvInfo cv, string? baseUrl = null)
        where TResponse : CvDataResponse, new()
    {
        return new TResponse
        {
            Id = cv.Id,
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
            QualityScore = cv.QualityScore,
            QualityReason = cv.QualityReason,
            QualityDetails = cv.QualityDetails?.RootElement.Clone(),
            IsMarked = cv.IsMarked,
            Tag = cv.Tag,
            WorkType = cv.WorkType,
            Note = cv.Note,
            CvFile = cv.CvFile is null
                ? null
                : new CvFileDataResponse
                {
                    Id = cv.CvFile.Id,
                    UploadedBy = cv.CvFile.UploadedBy,
                    OriginalFileName = cv.CvFile.OriginalFileName,
                    FileUrl = BuildFileUrl(cv.CvFile.FileUrl, baseUrl),
                    FileType = cv.CvFile.FileType,
                    FileSize = cv.CvFile.FileSize,
                    AiDocumentId = cv.CvFile.AiDocumentId,
                    CreatedAt = cv.CvFile.CreatedAt,
                    UpdatedAt = cv.CvFile.UpdatedAt
                },
            CvSkills = cv.CvSkills
                .Select(skill => skill.Name)
                .ToArray(),
            CvCertifications = cv.CvCertifications
                .Select(certification => new CvCertificationDataResponse
                {
                    Id = certification.Id,
                    CvInfoId = certification.CvInfoId,
                    Name = certification.Name
                })
                .ToArray(),
            WorkExperiences = cv.WorkExperiences
                .Select(experience => new CvWorkExperienceDataResponse
                {
                    Id = experience.Id,
                    CvInfoId = experience.CvInfoId,
                    Company = experience.Company,
                    Position = experience.Position,
                    Duration = experience.Duration,
                    Responsibility = experience.Responsibility
                })
                .ToArray(),
            Scores = new CvSemanticScoreResponse
            {
                OfflineScore = cv.QualityScore,
                MatchingScore = null,
                FinalScore = null
            },
            Reasons = new CvSemanticReasonsResponse
            {
                OfflineReason = cv.QualityReason,
                MatchingReason = null,
                OverallConclusion = null
            }
        };
    }

    /// <summary>
    /// Chuyển đổi đường dẫn file local thành URL public thông qua baseUrl (ngrok).
    /// Nếu fileUrl đã là HTTP/HTTPS thì giữ nguyên.
    /// </summary>
    private static string BuildFileUrl(string fileUrl, string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
            return fileUrl;

        // Nếu đã là URL tuyệt đối (http/https) thì giữ nguyên
        if (Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            return fileUrl;

        // Nếu không có baseUrl, trả về path gốc
        if (string.IsNullOrWhiteSpace(baseUrl))
            return fileUrl;

        var fileName = Path.GetFileName(
            fileUrl.Replace('/', Path.DirectorySeparatorChar)
                   .Replace('\\', Path.DirectorySeparatorChar));

        return $"{baseUrl.TrimEnd('/')}/files/{fileName}";
    }
}
