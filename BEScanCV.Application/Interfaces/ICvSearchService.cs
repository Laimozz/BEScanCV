using BEScanCV.Application.DTOS;

namespace BEScanCV.Application.Interfaces;

public interface ICvSearchService
{
    Task<CvSearchResponse> SearchAsync(CvSearchRequest request, CancellationToken cancellationToken = default);
}
