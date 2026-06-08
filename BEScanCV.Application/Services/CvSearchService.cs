using System.Globalization;
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

    public async Task<CvSearchResponse> SearchAsync(CvSearchRequest request, CancellationToken cancellationToken = default)
    {
        var page = NormalizePage(request.Page);

        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return CreatePagedResponse(page, []);
        }

        var criteria = await searchQueryParser.ParseAsync(request.Query, cancellationToken);
        if (criteria.Fields.Count == 0)
        {
            return CreatePagedResponse(page, []);
        }

        var cvs = await cvInfoRepository.GetWithSkillsAsync(cancellationToken);

        var rankedResults = cvs
            .Select(cv =>
            {
                var matchedCriteria = GetMatchedCriteria(criteria, cv);
                var candidateSkills = cv.CvSkills
                    .Select(cvSkill => cvSkill.Skill?.Name)
                    .Where(skill => !string.IsNullOrWhiteSpace(skill))
                    .Select(skill => skill!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                return new
                {
                    Score = matchedCriteria.Length,
                    Result = new CvSearchResultDto(
                        cv.FullName,
                        cv.Email,
                        candidateSkills,
                        cv.CreatedAt,
                        cv.CvFile?.UploadedBy ?? 0)
                };
            })
            .Where(candidate => candidate.Score > 0)
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.Result.FullName)
            .Select(candidate => candidate.Result)
            .ToArray();

        return CreatePagedResponse(page, rankedResults);
    }

    private static CvSearchResponse CreatePagedResponse(int page, IReadOnlyCollection<CvSearchResultDto> rankedResults)
    {
        var totalItems = rankedResults.Count;
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)PageSize);
        var results = rankedResults
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToArray();

        return new CvSearchResponse(page, PageSize, totalItems, totalPages, results);
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
            "dateofbirth" => IsTextMatched(normalizedValue, cv.DateOfBirth?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            "address" => IsTextMatched(normalizedValue, cv.Address),
            "summary" => IsTextMatched(normalizedValue, cv.Summary),
            "education" or "educations" => cv.Educations.Any(item => IsTextMatched(normalizedValue, item)),
            "certification" or "certifications" => cv.Certifications.Any(item => IsTextMatched(normalizedValue, item)),
            "createdat" => IsTextMatched(normalizedValue, cv.CreatedAt.ToString("O", CultureInfo.InvariantCulture)),
            "updatedat" => IsTextMatched(normalizedValue, cv.UpdatedAt.ToString("O", CultureInfo.InvariantCulture)),
            "status" => IsTextMatched(normalizedValue, cv.Status),
            "uploadedby" => IsExactNumberMatched(value, cv.CvFile?.UploadedBy),
            "skill" or "skills" or "skillname" => cv.CvSkills.Any(cvSkill => IsTextMatched(normalizedValue, cvSkill.Skill?.Name)),
            "skillid" => cv.CvSkills.Any(cvSkill => IsExactNumberMatched(value, cvSkill.SkillId)),
            "confidencescore" => cv.CvSkills.Any(cvSkill => IsMinimumDecimalMatched(value, cvSkill.ConfidenceScore)),
            "exp" or "experience" or "yearsofexperience" => cv.CvSkills.Any(cvSkill => IsMinimumDecimalMatched(value, cvSkill.YearsOfExperience)),
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

    private static bool IsMinimumDecimalMatched(string value, decimal? candidate)
    {
        return candidate is not null &&
            decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number) &&
            candidate.Value >= number;
    }
}
