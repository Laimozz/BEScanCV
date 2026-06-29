using BEScanCV.Application.DTOS.Requests;
using BEScanCV.Application.DTOS.Response;

namespace BEScanCV.Application.Interfaces;

public interface ICvGetAllService
{
    Task<CvGetAllResponse> CvGetAllAsync(
        CvGetAllRequest request,
        CancellationToken cancellationToken = default);
}
