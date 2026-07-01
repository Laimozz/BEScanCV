using BEScanCV.Application.DTOS.Response;
using BEScanCV.Application.Interfaces;
using BEScanCV.Application.Interfaces.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BEScanCV.Application.Services;

public sealed class CvDetailService(
    ICvInfoRepository cvInfoRepository,
    IConfiguration configuration,
    ILogger<CvDetailService> logger)
    : ICvDetailService
{
    public async Task<CvDetailResponse?> GetByCvFileIdAsync(
        long cvFileId,
        CancellationToken cancellationToken = default)
    {
        if (cvFileId <= 0)
        {
            logger.LogWarning("GetByCvFileIdAsync called with invalid cvFileId: {CvFileId} at {Timestamp}", cvFileId, DateTime.UtcNow);
            throw new ArgumentException(
                "cv_file_id is invalid.",
                nameof(cvFileId));
        }

        var cv = await cvInfoRepository.GetByCvFileIdAsync(
            cvFileId,
            cancellationToken);

        if (cv is null)
        {
            logger.LogWarning("CV not found for detail. CvFileId: {CvFileId} at {Timestamp}", cvFileId, DateTime.UtcNow);
            return null;
        }

        var baseUrl = configuration["PublicBaseUrl"];
        logger.LogInformation("Retrieved CV detail. CvFileId: {CvFileId}, CvInfoId: {CvInfoId} at {Timestamp}", cvFileId, cv.Id, DateTime.UtcNow);

        return cv is null
            ? null
            : CvDataResponseMapper.Map<CvDetailResponse>(cv, baseUrl);
    }
}
