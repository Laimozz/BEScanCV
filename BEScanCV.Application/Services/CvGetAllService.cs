using System.Globalization;
using BEScanCV.Application.DTOS;
using BEScanCV.Application.Interfaces;
using BEScanCV.Application.Interfaces.Repositories;
using BEScanCV.Domain.Entities;

namespace BEScanCV.Application.Services;

public sealed class CvGetAllService(ICvInfoRepository cvInfoRepository) : ICvGetAllService
{
    public async Task<CvGetAllResponse> CvGetAllAsync(CvGetAllRequest request, CancellationToken cancellationToken = default)
    {
        var page = NormalizePage(request.Page);
        var limit = request.Limit > 0 ? request.Limit : 10;

        // Fetch records from database
        var cvs = await cvInfoRepository.GetWithSkillsAsync(cancellationToken);

        // Apply search filter if provided
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchTerm = Normalize(request.Search);
            cvs = cvs
                .Where(cv => MatchesSearch(cv, searchTerm))
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
                        cv.CvFile?.Uploader?.FullName ?? string.Empty));
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

    private static int NormalizePage(int page)
    {
        return page < 1 ? 1 : page;
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

        // Search in Skill names
        if (cv.CvSkills?.Any(cs => !string.IsNullOrWhiteSpace(cs.Name) && Normalize(cs.Name).Contains(searchTerm)) ?? false)
            return true;

        return false;
    }
}
