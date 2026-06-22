using BEScanCV.Application.DTOS;
using BEScanCV.Application.Interfaces;
using BEScanCV.Application.Interfaces.Repositories;
using Microsoft.Extensions.Configuration;

namespace BEScanCV.Application.Services;

public sealed class CvDetailService(
    ICvInfoRepository cvInfoRepository,
    IConfiguration configuration)
    : ICvDetailService
{
    public async Task<CvDetailResponse?> GetByCvFileIdAsync(
        long cvFileId,
        CancellationToken cancellationToken = default)
    {
        if (cvFileId <= 0)
        {
            throw new ArgumentException(
                "cv_file_id is invalid.",
                nameof(cvFileId));
        }

        var cv = await cvInfoRepository.GetByCvFileIdAsync(
            cvFileId,
            cancellationToken);

        var baseUrl = configuration["PublicBaseUrl"];

        return cv is null
            ? null
            : CvDataResponseMapper.Map<CvDetailResponse>(cv, baseUrl);
    }
}
