using System.Globalization;
using System.Text.Json;
using BEScanCV.Application.DTOS;
using BEScanCV.Application.Interfaces;
using BEScanCV.Application.Interfaces.Repositories;
using BEScanCV.Domain.Entities;

namespace BEScanCV.Application.Services;

public sealed class CvGetAllService(ICvInfoRepository cvInfoRepository) : ICvGetAllService
{
    public async Task<CvGetAllResponse> CvGetAllAsync(
        CvGetAllRequest request,
        string requestBaseUrl,
        long uploadedBy,
        CancellationToken cancellationToken = default)
    {
        var page = NormalizePage(request.Page);
        var limit = request.Limit > 0 ? request.Limit : 10;

        var cvs = await cvInfoRepository.GetWithSkillsAsync(
            uploadedBy,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchTerm = Normalize(request.Search);
            cvs = cvs
                .Where(cv => MatchesSearch(cv, searchTerm))
                .ToList();
        }

        if (request.Filter is not null)
        {
            cvs = cvs
                .Where(cv => MatchesFilter(cv, request.Filter))
                .ToList();
        }

        // Map and order the domain records into DTOs
        var mappedResults = cvs
            .Select(cv =>
            {
                var candidateSkills = cv.CvSkills
                    .Select(cvSkill => cvSkill.Name)
                    .Where(skill => !string.IsNullOrWhiteSpace(skill))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                return new CvSearchResultDto(
                    cv.FullName,
                    cv.Email,
                    cv.CvFileId,
                    candidateSkills,
                    cv.CreatedAt,
                    new CvUploaderDto(
                        cv.CvFile?.UploadedBy ?? 0,
                        cv.CvFile?.Uploader?.FullName ?? string.Empty),
                    BuildPdfUrl(cv.CvFile?.FileUrl, requestBaseUrl));
            })
            .OrderByDescending(cv => cv.CreatedAt) // Ensures a predictable sorted feed
            .ToArray();

        return CreatePagedResponse(page, limit, mappedResults);
    }
    private static CvGetAllResponse CreatePagedResponse(int page, int limit, IReadOnlyCollection<CvSearchResultDto> resultsCollection)
{
    var totalItems = resultsCollection.Count;
    var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)limit);
    
    var results = resultsCollection
        .Skip((page - 1) * limit)
        .Take(limit)
        .ToArray();

    var meta = new PaginationMetaDto(totalItems, page, limit, totalPages);
    
    return new CvGetAllResponse(results, meta);
}

    private static int NormalizePage(int page) => page < 1 ? 1 : page;

    private static string? BuildPdfUrl(string? fileUrl, string requestBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl)) return null;

        if (Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            return fileUrl;

        var fileName = Path.GetFileName(
            fileUrl.Replace('/', Path.DirectorySeparatorChar)
                   .Replace('\\', Path.DirectorySeparatorChar));
        return $"{requestBaseUrl.TrimEnd('/')}/files/{fileName}";
    }

   
    private static string Normalize(string value) => value.Trim().ToLowerInvariant();

    private static string NormalizeFieldName(string fieldName)
    {
        return Normalize(fieldName)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal);
    }

    private static bool MatchesSearch(CvInfo cv, string searchTerm)
    {
        // Search in FullName
        if (!string.IsNullOrWhiteSpace(cv.FullName) && Normalize(cv.FullName).Contains(searchTerm))
            return true;

        // Search in Email
        if (!string.IsNullOrWhiteSpace(cv.Email) && Normalize(cv.Email).Contains(searchTerm))
            return true;

        if (!string.IsNullOrWhiteSpace(cv.Position) && Normalize(cv.Position).Contains(searchTerm))
            return true;

        if (!string.IsNullOrWhiteSpace(cv.RawText) && Normalize(cv.RawText).Contains(searchTerm))
            return true;

        if (IsJsonTextMatched(searchTerm, cv.Educations) || IsJsonTextMatched(searchTerm, cv.ProfileData))
            return true;

        // Search in Skill names
        if (cv.CvSkills?.Any(cs => !string.IsNullOrWhiteSpace(cs.Name) && Normalize(cs.Name).Contains(searchTerm)) ?? false)
            return true;

        return false;
    }

    private static bool IsJsonTextMatched(string searchTerm, JsonDocument? value)
    {
        return value is not null && Normalize(value.RootElement.ToString()).Contains(searchTerm);
    }

    private static bool MatchesFilter(CvInfo cv, CvGetAllFilterDto filter)
    {
        if (filter.TotalExperienceYears.HasValue &&
            (!cv.TotalExperienceYears.HasValue || cv.TotalExperienceYears.Value < filter.TotalExperienceYears.Value))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(filter.Position) &&
            (string.IsNullOrWhiteSpace(cv.Position) || !Normalize(cv.Position).Contains(Normalize(filter.Position))))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(filter.Skills))
        {
            var requiredSkills = filter.Skills
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(Normalize)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();

            if (requiredSkills.Length > 0 &&
                !requiredSkills.All(required => cv.CvSkills?.Any(cs => 
                {
                    var normalizedSkill = Normalize(cs.Name);
                    return !string.IsNullOrWhiteSpace(normalizedSkill) && 
                           (normalizedSkill.Contains(required) || required.Contains(normalizedSkill));
                }) ?? false))
            {
                return false;
            }
        }

        return true;
    }
}
