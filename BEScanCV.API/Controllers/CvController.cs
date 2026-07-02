using BEScanCV.API.Common;
using BEScanCV.Application.DTOS.Requests;
using BEScanCV.Application.DTOS.Response;
using BEScanCV.Application.Exceptions;
using BEScanCV.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace BEScanCV.API.Controllers;

[ApiController]
[Route("api/v1/cvs")]
[Authorize]
public sealed class CvController(ICvService cvService, ILogger<CvController> logger) : ControllerBase
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

        logger.LogInformation("Received bulk upload request with {FileCount} files at {Timestamp}", files?.Count ?? 0, DateTime.UtcNow);

        try
        {
            var response = await cvService.BulkUploadAsync(request, cancellationToken);
            response.WebsocketEndpoint = BuildWebSocketEndpoint(response.WebsocketEndpoint);

            logger.LogInformation("Bulk upload accepted. BatchId: {BatchId}, AcceptedFiles: {AcceptedFiles} at {Timestamp}", response.BatchId, response.AcceptedFiles, DateTime.UtcNow);

            return Accepted(new ApiResponse<CvBulkUploadResponse>(response)
            {
                Message = "Files accepted for processing.",
                StatusCode = StatusCodes.Status202Accepted
            });
        }
        catch (CvUploadValidationException ex)
        {
            logger.LogWarning("Bulk upload validation failed at {Timestamp}: {Message}", DateTime.UtcNow, ex.Message);
            return StatusCode(ex.StatusCode, new ApiResponse<object>(null)
            {
                Message = ex.Message,
                Success = false,
                StatusCode = ex.StatusCode
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Upload CV failed at {Timestamp}", DateTime.UtcNow);
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
        var response = await cvService.GetBatchStatusAsync(batchId, cancellationToken);
        if (response is null)
        {
            logger.LogWarning("Batch not found. BatchId: {BatchId} at {Timestamp}", batchId, DateTime.UtcNow);
            return NotFound(new ApiResponse<CvBatchUploadStatusResponse>(null)
            {
                Message = "Batch not found.",
                Success = false,
                StatusCode = StatusCodes.Status404NotFound
            });
        }

        logger.LogInformation("Retrieved batch status. BatchId: {BatchId}, Status: {Status} at {Timestamp}", batchId, response.Status, DateTime.UtcNow);

        return Ok(new ApiResponse<CvBatchUploadStatusResponse>(response));
    }

    [HttpPost("bulk-upload/{batchId}/cancel")]
    public async Task<ActionResult<ApiResponse<CvBatchCancelResponse>>> CancelBatch(
        string batchId,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await cvService.CancelBatchAsync(batchId, cancellationToken);
            logger.LogInformation("Batch cancellation requested. BatchId: {BatchId}, Status: {Status} at {Timestamp}", batchId, response.Status, DateTime.UtcNow);
            return Ok(new ApiResponse<CvBatchCancelResponse>(response)
            {
                Message = "Batch cancellation requested."
            });
        }
        catch (CvUploadValidationException ex)
        {
            logger.LogWarning("Batch cancellation validation failed. BatchId: {BatchId} at {Timestamp}: {Message}", batchId, DateTime.UtcNow, ex.Message);
            return StatusCode(ex.StatusCode, new ApiResponse<object>(null)
            {
                Message = ex.Message,
                Success = false,
                StatusCode = ex.StatusCode
            });
        }
    }

    [HttpPut("{cvInfoId:long}")]
    public async Task<ActionResult<ApiResponse<CvUpdateResponse>>> UpdateCv(
        long cvInfoId,
        [FromBody] CvUpdateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await cvService.UpdateAsync(
                cvInfoId,
                request,
                cancellationToken);

            logger.LogInformation("CV updated successfully. CvInfoId: {CvInfoId} at {Timestamp}", cvInfoId, DateTime.UtcNow);

            return Ok(new ApiResponse<CvUpdateResponse>(response)
            {
                Message = "CV updated successfully."
            });
        }
        catch (CvUploadValidationException ex)
        {
            logger.LogWarning("CV update validation failed. CvInfoId: {CvInfoId} at {Timestamp}: {Message}", cvInfoId, DateTime.UtcNow, ex.Message);
            return StatusCode(ex.StatusCode, new ApiResponse<object>(null)
            {
                Message = ex.Message,
                Success = false,
                StatusCode = ex.StatusCode
            });
        }
    }

    [HttpDelete("{cvInfoId:long}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteCv(
        long cvInfoId,
        CancellationToken cancellationToken)
    {
        try
        {
            await cvService.DeleteAsync(cvInfoId, cancellationToken);

            logger.LogInformation("CV deleted successfully. CvInfoId: {CvInfoId} at {Timestamp}", cvInfoId, DateTime.UtcNow);

            return Ok(new ApiResponse<object>(null)
            {
                Message = "CV deleted successfully.",
                StatusCode = 204
            });
        }
        catch (CvUploadValidationException ex)
        {
            logger.LogWarning("CV deletion validation failed. CvInfoId: {CvInfoId} at {Timestamp}: {Message}", cvInfoId, DateTime.UtcNow, ex.Message);
            return StatusCode(ex.StatusCode, new ApiResponse<object>(null)
            {
                Message = ex.Message,
                Success = false,
                StatusCode = ex.StatusCode
            });
        }
    }

    [AllowAnonymous]
    [HttpPost("quality-score")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateQualityScore(
        [FromBody] CvQualityScoreRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await cvService.UpdateQualityScoreAsync(
                request,
                cancellationToken);

            logger.LogInformation("CV quality score updated successfully. CvId: {CvId}, Score: {QualityScore} at {Timestamp}", request.CvId, request.QualityScore, DateTime.UtcNow);

            return Ok(new ApiResponse<object>(null)
            {
                Message = "CV quality score updated successfully."
            });
        }
        catch (CvUploadValidationException ex)
        {
            logger.LogWarning("CV quality score update validation failed at {Timestamp}: {Message}", DateTime.UtcNow, ex.Message);
            return StatusCode(ex.StatusCode, new ApiResponse<object>(null)
            {
                Message = ex.Message,
                Success = false,
                StatusCode = ex.StatusCode
            });
        }
    }
    [AllowAnonymous]
    [HttpPost("quality-scores")]
    public async Task<ActionResult<CvQualityScoresResponse>> GetQualityScores(
        [FromBody] CvQualityScoresRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await cvService.GetQualityScoresAsync(
                request,
                cancellationToken);

            logger.LogInformation("Retrieved quality scores for {CvCount} CVs at {Timestamp}", response.Count, DateTime.UtcNow);

            return Ok(new CvQualityScoresResponse
            {
                Data = response
            });
        }
        catch (CvUploadValidationException ex)
        {
            logger.LogWarning("CV quality scores retrieval validation failed at {Timestamp}: {Message}", DateTime.UtcNow, ex.Message);
            return StatusCode(ex.StatusCode, new ApiResponse<object>(null)
            {
                Message = ex.Message,
                Success = false,
                StatusCode = ex.StatusCode
            });
        }
    }

    private string BuildWebSocketEndpoint(string endpoint)
    {
        var scheme = Request.IsHttps ? "wss" : "ws";
        var path = endpoint.StartsWith('/') ? endpoint : $"/{endpoint}";

        return $"{scheme}://{Request.Host}{Request.PathBase}{path}";
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

}
