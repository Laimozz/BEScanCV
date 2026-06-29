using BEScanCV.Application.DTOS;
using BEScanCV.Application.DTOS.Requests;
using BEScanCV.Application.DTOS.Response;
using BEScanCV.Application.Interfaces;
using BEScanCV.Application.Interfaces.Repositories;
using Microsoft.Extensions.Configuration;

namespace BEScanCV.Application.Services;

public sealed class CvGetAllService(
    ICvInfoRepository cvInfoRepository,
    IConfiguration configuration) : ICvGetAllService
{
    public async Task<CvGetAllResponse> CvGetAllAsync(
        CvGetAllRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = NormalizePage(request.Page);
        var limit = request.Limit > 0 ? request.Limit : 10;

        var (cvs, total) = await cvInfoRepository.GetPagedAsync(
            page, limit, request.Search, request.Filter, cancellationToken);

        var baseUrl = configuration["PublicBaseUrl"];
        var items = cvs
            .Select(cv => CvDataResponseMapper.Map<CvGetAllItemResponse>(cv, baseUrl))
            .ToArray();

        var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)limit);
        var meta = new PaginationMetaDto(total, page, limit, totalPages);
        return new CvGetAllResponse(items, meta);
    }

    private static int NormalizePage(int page) => page < 1 ? 1 : page;
}