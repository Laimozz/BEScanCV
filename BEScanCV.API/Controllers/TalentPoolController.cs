using BEScanCV.API.Common;
using BEScanCV.Application.DTOS.Requests;
using BEScanCV.Application.DTOS.Response;
using BEScanCV.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BEScanCV.API.Controllers;

[ApiController]
[Route("api/v1/cvs")]
[Authorize]
public sealed class TalentPoolController(ITalentPoolService talentPoolService, ILogger<TalentPoolController> logger) : ControllerBase
{
    /// <summary>
    /// POST /api/v1/cvs/talent-pool
    /// Lấy danh sách CV đã đánh dấu vào Talent Pool (is_marked = true), có phân trang.
    /// </summary>
    [HttpPost("talent-pool")]
    public async Task<ActionResult<ApiResponse<TalentPoolResponse>>> GetTalentPool(
        [FromBody] TalentPoolRequest request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Retrieving talent pool. Page: {Page}, Limit: {Limit} at {Timestamp}", request.Page, request.Limit, DateTime.UtcNow);
        var response = await talentPoolService.GetTalentPoolAsync(request, cancellationToken);
        logger.LogInformation("Retrieved talent pool. TotalItems: {TotalItems} at {Timestamp}", response.Meta.Total, DateTime.UtcNow);
        return Ok(new ApiResponse<TalentPoolResponse>(response));
    }

    /// <summary>
    /// PATCH /api/v1/cvs/{id}/talent
    /// Đánh dấu (is_marked=true) hoặc bỏ đánh dấu (is_marked=false) một CV trong Talent Pool.
    /// </summary>
    [HttpPatch("{id:long}/talent")]
    public async Task<ActionResult<ApiResponse<object>>> MarkTalent(
        long id,
        [FromBody] MarkTalentRequest request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Marking talent. CvInfoId: {CvInfoId}, IsMarked: {IsMarked} at {Timestamp}", id, request.IsMarked, DateTime.UtcNow);
        try
        {
            var (isMarked, data) = await talentPoolService.MarkTalentAsync(
                id,
                request.IsMarked,
                cancellationToken);

            var message = isMarked
                ? "CV marked as talent successfully"
                : "CV removed from talent pool";

            logger.LogInformation("CvInfoId: {CvInfoId} talent status updated. IsMarked: {IsMarked} at {Timestamp}", id, isMarked, DateTime.UtcNow);
            return Ok(new ApiResponse<object>(data) { Message = message });
        }
        catch (KeyNotFoundException)
        {
            logger.LogWarning("CV not found for talent marking. CvInfoId: {CvInfoId} at {Timestamp}", id, DateTime.UtcNow);
            return NotFound(new ApiResponse<object>(null)
            {
                Success = false,
                StatusCode = StatusCodes.Status404NotFound,
                Message = "CV not found"
            });
        }
    }
}
