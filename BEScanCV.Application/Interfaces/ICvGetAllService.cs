using BEScanCV.Application.DTOS;

namespace BEScanCV.Application.Interfaces;

public interface ICvGetAllService
{
    Task<CvGetAllResponse> CvGetAllAsync(
        CvGetAllRequest request,
        string requestBaseUrl,
        CancellationToken cancellationToken = default);
}
