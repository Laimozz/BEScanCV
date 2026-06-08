using BEScanCV.Application.DTOS;

namespace BEScanCV.Application.Interfaces;

public interface ISearchQueryParser
{
    Task<CvSearchCriteriaDto> ParseAsync(string query, CancellationToken cancellationToken = default);
}
