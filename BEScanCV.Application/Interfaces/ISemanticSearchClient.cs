using BEScanCV.Application.DTOS;

namespace BEScanCV.Application.Interfaces;

public interface ISemanticSearchClient
{
    Task<IReadOnlyCollection<AiSemanticSearchResult>> SearchAsync(
        CvSemanticSearchRequest request,
        CancellationToken cancellationToken = default);
}
