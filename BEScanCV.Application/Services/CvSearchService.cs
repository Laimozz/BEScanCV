using System.Globalization;
using System.Text.Json;
using BEScanCV.Application.DTOS;
using BEScanCV.Application.Interfaces;
using BEScanCV.Application.Interfaces.Repositories;
using BEScanCV.Domain.Entities;

namespace BEScanCV.Application.Services;

public sealed class CvSearchService(
    ICvInfoRepository cvInfoRepository,
    ISearchQueryParser searchQueryParser) : ICvSearchService
{
    private const int PageSize = 10;

    public async Task<CvSearchResponse> SearchAsync(CvSearchRequest request, string requestBaseUrl, CancellationToken cancellationToken = default)
    {
        var page = NormalizePage(request.Page);
        var limit = request.Limit > 0 ? request.Limit : 10; // Default limit fallback

        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return CreatePagedResponse(page, limit, []);
        }

        var criteria = await searchQueryParser.ParseAsync(request.Query, cancellationToken);
        if (criteria.Fields.Count == 0)
        {
            return CreatePagedResponse(page, limit, []);
        }

        var cvs = await cvInfoRepository.GetWithSkillsAsync(cancellationToken);

        var rankedResults = cvs
            .Select(cv =>
            {
                var matchedCriteria = GetMatchedCriteria(criteria, cv);
                var candidateSkills = cv.CvSkills
                    .Select(cvSkill => cvSkill.Name)
                    .Where(skill => !string.IsNullOrWhiteSpace(skill))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                return new
                {
                    Score = matchedCriteria.Length,
                    Result = CreateResult(cv, candidateSkills, requestBaseUrl)
                };
            })
            .Where(candidate => candidate.Score > 0)
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.Result.FullName)
            .Select(candidate => candidate.Result)
            .ToArray();

        return CreatePagedResponse(page, limit, rankedResults);
    }

    private static CvSearchResponse CreatePagedResponse(int page, int limit, IReadOnlyCollection<CvSearchResultDto> rankedResults)
    {
        var totalItems = rankedResults.Count;
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)limit);
        var results = rankedResults
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToArray();

        var meta = new PaginationMetaDto(totalItems, page, limit, totalPages);
        
        return new CvSearchResponse(results, meta);
    }

    private static int NormalizePage(int page)
    {
        return page < 1 ? 1 : page;
    }

    private static string[] GetMatchedCriteria(CvSearchCriteriaDto criteria, CvInfo cv)
    {
        var matchedCriteria = new List<string>();

        foreach (var field in criteria.Fields)
        {
            foreach (var value in field.Value.Where(value => !string.IsNullOrWhiteSpace(value)))
            {
                if (IsCriteriaMatched(field.Key, value, cv))
                {
                    matchedCriteria.Add($"{field.Key}: {value}");
                }

            }
        }

        return matchedCriteria.ToArray();
    }

    private static bool IsCriteriaMatched(string fieldName, string value, CvInfo cv)
    {
        var normalizedFieldName = NormalizeFieldName(fieldName);
        var normalizedValue = Normalize(value);

        return normalizedFieldName switch
        {
            "id" or "cvinfoid" => IsExactNumberMatched(value, cv.Id),
            "cvfileid" => IsExactNumberMatched(value, cv.CvFileId),
            "fullname" => IsTextMatched(normalizedValue, cv.FullName),
            "email" => IsTextMatched(normalizedValue, cv.Email),
            "phone" => IsTextMatched(normalizedValue, cv.Phone),
            "position" => IsTextMatched(normalizedValue, cv.Position),
            "dateofbirth" => IsTextMatched(normalizedValue, cv.DateOfBirth?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            "address" => IsTextMatched(normalizedValue, cv.Address),
            "summary" => IsTextMatched(normalizedValue, cv.Summary),
            "rawtext" => IsTextMatched(normalizedValue, cv.RawText),
            "profiledata" => IsJsonTextMatched(normalizedValue, cv.ProfileData),
            "education" or "educations" => IsJsonTextMatched(normalizedValue, cv.Educations),
            "createdat" => IsTextMatched(normalizedValue, cv.CreatedAt.ToString("O", CultureInfo.InvariantCulture)),
            "updatedat" => IsTextMatched(normalizedValue, cv.UpdatedAt.ToString("O", CultureInfo.InvariantCulture)),
            "status" => IsTextMatched(normalizedValue, cv.Status),
            "uploadedby" => IsExactNumberMatched(value, cv.CvFile?.UploadedBy),
            "skill" or "skills" or "skillname" => cv.CvSkills.Any(cvSkill => IsTextMatched(normalizedValue, cvSkill.Name)),
            "exp" or "experience" or "totalexperienceyears" => IsMinimumIntMatched(value, cv.TotalExperienceYears),
            _ => false
        };
    }

    private static bool IsTextMatched(string normalizedKeyword, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalizedValue = Normalize(value);
        return normalizedValue.Contains(normalizedKeyword, StringComparison.OrdinalIgnoreCase) ||
            normalizedKeyword.Contains(normalizedValue, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsJsonTextMatched(string normalizedKeyword, JsonDocument? value)
    {
        if (value is null)
        {
            return false;
        }

        return IsTextMatched(normalizedKeyword, value.RootElement.ToString());
    }

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();

    private static string NormalizeFieldName(string fieldName)
    {
        return Normalize(fieldName)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal);
    }

    private static bool IsExactNumberMatched(string value, long? candidate)
    {
        return candidate is not null &&
            long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) &&
            candidate.Value == number;
    }

    private static bool IsMinimumIntMatched(string value, int? candidate)
    {
        return candidate is not null &&
            int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) &&
            candidate.Value >= number;
    }

    private static CvSearchResultDto CreateResult(CvInfo cv, string[] candidateSkills, string requestBaseUrl)
    {
        var uploader = cv.CvFile?.Uploader;
        return new CvSearchResultDto(
            cv.FullName,
            cv.Email,
            cv.CvFileId,
            candidateSkills,
            cv.CreatedAt,
            new CvUploaderDto(
                cv.CvFile?.UploadedBy ?? 0,
                uploader?.FullName ?? string.Empty),
            BuildPdfUrl(cv.CvFile?.FileUrl, requestBaseUrl));
    }

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
}
