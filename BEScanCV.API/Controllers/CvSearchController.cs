using BEScanCV.API.Common;
using BEScanCV.Application.DTOS.Requests;
using BEScanCV.Application.DTOS.Response;
using BEScanCV.Application.Exceptions;
using BEScanCV.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BEScanCV.API.Controllers;

[ApiController]
[Route("api/v1/cvs")]
[Authorize]
public sealed class CvSearchController(ICvSearchService cvSearchService, ILogger<CvSearchController> logger) : ControllerBase
{
    // [HttpPost("search")]
    // public async Task<ActionResult<ApiResponse<CvSearchResponse>>> Search(
    //     [FromBody] CvSearchRequest request,
    //     CancellationToken cancellationToken)
    // {
    //     if (string.IsNullOrWhiteSpace(request.Query))
    //     {
    //         return BadRequest(new ApiResponse<object>(null)
    //         {
    //             Message = "Query is required.",
    //             Success = false,
    //             StatusCode = StatusCodes.Status400BadRequest
    //         });
    //     }

    //     try
    //     {
    //         var response = await cvSearchService.SearchAsync(
    //             request,
    //             GetRequestBaseUrl(),
    //             cancellationToken);

    //         return Ok(new ApiResponse<CvSearchResponse>(response));
    //     }
    //     catch (AiParserException ex)
    //     {
    //         return CreateAiErrorResponse(ex);
    //     }
    // }

    // [HttpGet("get-favor")]  //    [HttpGet("favourites")], in api endpoint, dont use verb

    // public async Task<ActionResult<ApiResponse<IReadOnlyCollection<CvFavoriteResponse>>>> GetFavorites(
    //     CancellationToken cancellationToken)
    // {
    //     var response = await cvSearchService.GetFavoritesAsync(cancellationToken);

    //     return Ok(new ApiResponse<IReadOnlyCollection<CvFavoriteResponse>>(response));
    // }

    [HttpPost("search")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<CvSearchSemanticResponse>>>> SemanticSearch(
        [FromBody] CvSemanticSearchRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            logger.LogWarning("Semantic search failed. Query is empty at {Timestamp}", DateTime.UtcNow);
            return BadRequest(new ApiResponse<object>(null)
            {
                Message = "Query is required.",
                Success = false,
                StatusCode = StatusCodes.Status400BadRequest
            });
        }

        if (request.TopK is <= 0)
        {
            logger.LogWarning("Semantic search failed. topK must be greater than zero at {Timestamp}", DateTime.UtcNow);
            return BadRequest(new ApiResponse<object>(null)
            {
                Message = "topK must be greater than zero.",
                Success = false,
                StatusCode = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            logger.LogInformation("Semantic search received. Query: {Query}, TopK: {TopK} at {Timestamp}", request.Query, request.TopK, DateTime.UtcNow);

            var response = await cvSearchService.SemanticSearchAsync(
                request,
                cancellationToken);

            logger.LogInformation("Semantic search completed. Results: {ResultCount} at {Timestamp}", response.Count, DateTime.UtcNow);

            return Ok(
                new ApiResponse<IReadOnlyCollection<CvSearchSemanticResponse>>(
                    response));
        }
        catch (AiParserException ex)
        {
            logger.LogWarning("Semantic search failed. StatusCode: {StatusCode} at {Timestamp}", ex.StatusCode, DateTime.UtcNow);
            return CreateAiErrorResponse(ex);
        }
    }

    private ObjectResult CreateAiErrorResponse(AiParserException ex) =>
        StatusCode(ex.StatusCode, new
        {
            error = new
            {
                message = ex.ResponseBody,
                status = ex.StatusCode,
                timestamp = DateTime.UtcNow.ToString("O"),
                path = HttpContext.Request.Path.Value
            }
        });

    private string GetRequestBaseUrl() =>
        $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
}
