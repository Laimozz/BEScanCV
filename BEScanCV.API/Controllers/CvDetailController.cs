using BEScanCV.API.Common;
using BEScanCV.Application.DTOS.Response;
using BEScanCV.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BEScanCV.API.Controllers;

[ApiController]
[Route("api/v1/cvs")]
[Authorize]
public sealed class CvDetailController(ICvDetailService cvDetailService, ILogger<CvDetailController> logger) : ControllerBase
{
    /// <summary>
    /// Lấy toàn bộ thông tin CV kèm pdf_url để FE hiển thị file PDF trực tiếp.
    /// </summary>
    [HttpGet("{cvFileId:long}")]
    public async Task<ActionResult<ApiResponse<CvDetailResponse>>> GetCvDetail(
        long cvFileId,
        CancellationToken cancellationToken)
    {
        if (cvFileId <= 0)
        {
            logger.LogWarning("GetCvDetail called with invalid cv_file_id {CvFileId} at {Timestamp}", cvFileId, DateTime.UtcNow);
            return BadRequest(new ApiResponse<CvDetailResponse>(null)
            {
                Message = "cv_file_id is invalid.",
                Success = false,
                StatusCode = StatusCodes.Status400BadRequest
            });
        }

        var cv = await cvDetailService.GetByCvFileIdAsync(
            cvFileId,
            cancellationToken);

        if (cv is null)
        {
            logger.LogWarning("CV not found for cv_file_id {CvFileId} at {Timestamp}", cvFileId, DateTime.UtcNow);
            return NotFound(new ApiResponse<CvDetailResponse>(null)
            {
                Message = "CV not found.",
                Success = false,
                StatusCode = StatusCodes.Status404NotFound
            });
        }

        return Ok(new ApiResponse<CvDetailResponse>(cv));
    }
}
