using BEScanCV.Application.DTOS;

namespace BEScanCV.Application.Interfaces;

public interface ICvSearchService
{
    Task<CvSearchResponse> SearchAsync(
        CvSearchRequest request,
        string requestBaseUrl,
        long uploadedBy,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CvFavoriteResponse>> GetFavoritesAsync(
        long uploadedBy,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CvSearchSemanticResponse>> SemanticSearchAsync(
        CvSemanticSearchRequest request,
        long uploadedBy,
        CancellationToken cancellationToken = default);
}
