using BEScanCV.API.Common;
using BEScanCV.Application.DTOS;
using BEScanCV.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BEScanCV.API.Controllers;

[ApiController]
[Route("api/v1/cvs")]
[Authorize]
public sealed class TalentPoolController(ITalentPoolService talentPoolService) : ControllerBase
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
        var response = await talentPoolService.GetTalentPoolAsync(request, cancellationToken);
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
        try
        {
            var (isMarked, data) = await talentPoolService.MarkTalentAsync(
                id,
                request.IsMarked,
                cancellationToken);

            var message = isMarked
                ? "CV marked as talent successfully"
                : "CV removed from talent pool";

            return Ok(new ApiResponse<object>(data) { Message = message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ApiResponse<object>(null)
            {
                Success = false,
                StatusCode = StatusCodes.Status404NotFound,
                Message = "CV not found"
            });
        }
    }
}
