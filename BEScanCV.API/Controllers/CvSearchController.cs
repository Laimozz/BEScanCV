using BEScanCV.API.Common;
using BEScanCV.Application.DTOS.Requests;
using BEScanCV.Application.DTOS.Response;
using BEScanCV.Application.Exceptions;
using BEScanCV.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BEScanCV.API.Controllers;

[ApiController]
[Route("api/v1/cvs")]
[Authorize]
public sealed class CvSearchController(ICvSearchService cvSearchService) : ControllerBase
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

    [HttpGet("get-favor")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<CvFavoriteResponse>>>> GetFavorites(
        CancellationToken cancellationToken)
    {
        var response = await cvSearchService.GetFavoritesAsync(cancellationToken);

        return Ok(new ApiResponse<IReadOnlyCollection<CvFavoriteResponse>>(response));
    }

    [HttpPost("search")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<CvSearchSemanticResponse>>>> SemanticSearch(
        [FromBody] CvSemanticSearchRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest(new ApiResponse<object>(null)
            {
                Message = "Query is required.",
                Success = false,
                StatusCode = StatusCodes.Status400BadRequest
            });
        }

        if (request.TopK is <= 0)
        {
            return BadRequest(new ApiResponse<object>(null)
            {
                Message = "topK must be greater than zero.",
                Success = false,
                StatusCode = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            var response = await cvSearchService.SemanticSearchAsync(
                request,
                cancellationToken);

            return Ok(
                new ApiResponse<IReadOnlyCollection<CvSearchSemanticResponse>>(
                    response));
        }
        catch (AiParserException ex)
        {
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
