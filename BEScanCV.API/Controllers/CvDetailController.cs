using BEScanCV.API.Common;
using BEScanCV.API.Extensions;
using BEScanCV.Application.DTOS;
using BEScanCV.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BEScanCV.API.Controllers;

[ApiController]
[Route("api/v1/cvs")]
[Authorize]
public sealed class CvDetailController(ICvDetailService cvDetailService) : ControllerBase
{
    /// <summary>
    /// Lấy toàn bộ thông tin CV kèm pdf_url để FE hiển thị file PDF trực tiếp.
    /// </summary>
    [HttpGet("{cvFileId:long}")]
    public async Task<ActionResult<ApiResponse<CvDetailResponse>>> GetCvDetail(
        long cvFileId,
        CancellationToken cancellationToken)
    {
        var uploadedBy = User.GetCurrentUserId();
        if (uploadedBy is null)
            return Unauthorized(new ApiResponse<object>(null)
            {
                Message = "Authenticated user is required.",
                Success = false,
                StatusCode = StatusCodes.Status401Unauthorized
            });

        if (cvFileId <= 0)
            return BadRequest(new ApiResponse<CvDetailResponse>(null)
            {
                Message = "cv_file_id is invalid.",
                Success = false,
                StatusCode = StatusCodes.Status400BadRequest
            });

        var cv = await cvDetailService.GetByCvFileIdAsync(
            cvFileId,
            GetRequestBaseUrl(),
            uploadedBy.Value,
            cancellationToken);

        if (cv is null)
            return NotFound(new ApiResponse<CvDetailResponse>(null)
            {
                Message = "CV not found.",
                Success = false,
                StatusCode = StatusCodes.Status404NotFound
            });

        return Ok(new ApiResponse<CvDetailResponse>(cv));
    }

    private string GetRequestBaseUrl() =>
        $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
}
