namespace BEScanCV.Application.DTOS;

public sealed record CvSearchResponse(
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages,
    IReadOnlyCollection<CvSearchResultDto> Results);
