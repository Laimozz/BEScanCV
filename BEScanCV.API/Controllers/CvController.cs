using System.Security.Claims;
using BEScanCV.API.Common;
using BEScanCV.Application.DTOS;
using BEScanCV.Application.Exceptions;
using BEScanCV.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BEScanCV.API.Controllers;

[ApiController]
[Route("api/v1/cvs")]
public sealed class CvController(ICvUploadService cvUploadService) : ControllerBase
{
    [HttpPost("bulk-upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(110 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<CvBulkUploadResponse>>> BulkUpload(
        [FromForm] List<IFormFile>? files,
        [FromForm] string requestId,
        [FromForm] string? batchId,
        CancellationToken cancellationToken)
    {
        var request = new CvBulkUploadRequest(
            (files ?? []).Select(file => new CvBulkUploadFileInput(
                    file.FileName,
                    file.ContentType,
                    file.Length,
                    file.OpenReadStream))
                .ToArray(),
            requestId,
            batchId,
            GetCurrentUserId());

        try
        {
            var response = await cvUploadService.BulkUploadAsync(request, cancellationToken);
            response.WebsocketEndpoint = BuildWebSocketEndpoint(response.WebsocketEndpoint);

            return Accepted(new ApiResponse<CvBulkUploadResponse>(response)
            {
                Message = "Files accepted for processing.",
                StatusCode = StatusCodes.Status202Accepted
            });
        }
        catch (CvUploadValidationException ex)
        {
            return StatusCode(ex.StatusCode, new ApiResponse<object>(null)
            {
                Message = ex.Message,
                Success = false,
                StatusCode = ex.StatusCode
            });
        }
        catch
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>(null)
            {
                Message = "Upload CV failed.",
                Success = false,
                StatusCode = StatusCodes.Status500InternalServerError
            });
        }
    }

    [HttpGet("bulk-upload/{batchId}")]
    public async Task<ActionResult<ApiResponse<CvBatchUploadStatusResponse>>> GetBatchStatus(
        string batchId,
        CancellationToken cancellationToken)
    {
        var response = await cvUploadService.GetBatchStatusAsync(batchId, cancellationToken);
        if (response is null)
        {
            return NotFound(new ApiResponse<CvBatchUploadStatusResponse>(null)
            {
                Message = "Batch not found.",
                Success = false,
                StatusCode = StatusCodes.Status404NotFound
            });
        }

        return Ok(new ApiResponse<CvBatchUploadStatusResponse>(response));
    }

    [HttpPost("bulk-upload/{batchId}/cancel")]
    public async Task<ActionResult<ApiResponse<CvBatchCancelResponse>>> CancelBatch(
        string batchId,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await cvUploadService.CancelBatchAsync(batchId, cancellationToken);
            return Ok(new ApiResponse<CvBatchCancelResponse>(response)
            {
                Message = "Batch cancellation requested."
            });
        }
        catch (CvUploadValidationException ex)
        {
            return StatusCode(ex.StatusCode, new ApiResponse<object>(null)
            {
                Message = ex.Message,
                Success = false,
                StatusCode = ex.StatusCode
            });
        }
    }

    private long? GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                     User.FindFirstValue("sub") ??
                     User.FindFirstValue("user_id") ??
                     User.FindFirstValue("id");

        return long.TryParse(userId, out var parsedUserId) && parsedUserId > 0
            ? parsedUserId
            : null;
    }

    private string BuildWebSocketEndpoint(string endpoint)
    {
        var scheme = Request.IsHttps ? "wss" : "ws";
        var path = endpoint.StartsWith('/') ? endpoint : $"/{endpoint}";

        return $"{scheme}://{Request.Host}{Request.PathBase}{path}";
    }
}
