using BEScanCV.Application.DTOS.Requests;
using BEScanCV.Application.DTOS.Response;

namespace BEScanCV.Application.Interfaces;

public interface ICvSearchService
{
    Task<CvSearchResponse> SearchAsync(
        CvSearchRequest request,
        string requestBaseUrl,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CvFavoriteResponse>> GetFavoritesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CvSearchSemanticResponse>> SemanticSearchAsync(
        CvSemanticSearchRequest request,
        CancellationToken cancellationToken = default);
}
