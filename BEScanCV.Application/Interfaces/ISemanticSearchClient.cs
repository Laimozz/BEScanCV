using BEScanCV.Application.DTOS.Requests;
using BEScanCV.Application.DTOS.Response;

namespace BEScanCV.Application.Interfaces;

public interface ISemanticSearchClient
{
    Task<IReadOnlyCollection<AiSemanticSearchResult>> SearchAsync(
        CvSemanticSearchRequest request,
        CancellationToken cancellationToken = default);
}
