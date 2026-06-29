using BEScanCV.Application.DTOS;
using BEScanCV.Application.DTOS.Requests;
using BEScanCV.Application.DTOS.Response;
using BEScanCV.Application.Interfaces;
using BEScanCV.Application.Interfaces.Repositories;
using Microsoft.Extensions.Configuration;

namespace BEScanCV.Application.Services;

public sealed class TalentPoolService(
    ICvInfoRepository cvInfoRepository,
    IConfiguration configuration) : ITalentPoolService
{
    public async Task<TalentPoolResponse> GetTalentPoolAsync(
        TalentPoolRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var limit = request.Limit > 0 ? request.Limit : 10;

        var (cvs, total) = await cvInfoRepository.GetFavoritesAsync(page, limit, cancellationToken);

        var baseUrl = configuration["PublicBaseUrl"];
        var items = cvs
            .Select(cv => CvDataResponseMapper.Map<TalentPoolItemResponse>(cv, baseUrl))
            .ToArray();

        var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)limit);
        var meta = new PaginationMetaDto(total, page, limit, totalPages);
        return new TalentPoolResponse(items, meta);
    }

    public async Task<(bool isMarked, object data)> MarkTalentAsync(
        long cvInfoId,
        bool isMarked,
        CancellationToken cancellationToken = default)
    {
        var cv = await cvInfoRepository.GetByIdAsync(cvInfoId, cancellationToken)
            ?? throw new KeyNotFoundException($"CV with id {cvInfoId} not found.");

        cv.IsMarked = isMarked;
        cv.UpdatedAt = DateTime.UtcNow;

        await cvInfoRepository.UpdateAsync(cv, cancellationToken);

        var baseUrl = configuration["PublicBaseUrl"];
        object response = isMarked
            ? CvDataResponseMapper.Map<MarkTalentResponse>(cv, baseUrl)
            : new UnmarkTalentResponse
            {
                CvInfosId = cv.Id,
                IsMarked = cv.IsMarked,
                UpdatedAt = cv.UpdatedAt
            };

        return (isMarked, response);
    }
}
