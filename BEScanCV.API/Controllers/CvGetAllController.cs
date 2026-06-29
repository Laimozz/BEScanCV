using BEScanCV.API.Common;
using BEScanCV.Application.DTOS.Requests;
using BEScanCV.Application.DTOS.Response;
using BEScanCV.Application.Exceptions;
using BEScanCV.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BEScanCV.API.Controllers;

[ApiController]
[Route("api/v1/cvs/getAll")] // Why post instead of get ?
[Authorize]
public sealed class CvGetAllController(ICvGetAllService cvGetAllService, ILogger<CvGetAllController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CvGetAllResponse>>> GetAll (
        [FromBody] CvGetAllRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await cvGetAllService.CvGetAllAsync(
                request,
                cancellationToken);
            return Ok(new ApiResponse<CvGetAllResponse>(response));
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Operation canceled during CV GetAll. Path: {Path}",
                HttpContext?.Request.Path.Value);

            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiResponse<object>(null)
            {
                Message = "The operation was canceled due to a server-side timeout. Please try again.",
                Success = false,
                StatusCode = StatusCodes.Status503ServiceUnavailable
            });
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "Resource not found during CV GetAll. Path: {Path}",
                HttpContext?.Request.Path.Value);

            return NotFound(new ApiResponse<object>(null)
            {
                Message = ex.Message,
                Success = false,
                StatusCode = StatusCodes.Status404NotFound
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error in CV GetAll. Path: {Path}",
                HttpContext?.Request.Path.Value);

            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>(null)
            {
                Message = "An unexpected error occurred. Please contact support if the problem persists.",
                Success = false,
                StatusCode = StatusCodes.Status500InternalServerError
            });
        }
    }
}
