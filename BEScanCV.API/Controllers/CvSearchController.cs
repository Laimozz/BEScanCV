using BEScanCV.API.Common;
using BEScanCV.Application.DTOS;
using BEScanCV.Application.Exceptions;
using BEScanCV.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BEScanCV.API.Controllers;

[ApiController]
[Route("api/v1/cvs/search")]
public sealed class CvSearchController(ICvSearchService cvSearchService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CvSearchResponse>>> Search(
        [FromBody] CvSearchRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest(new ApiResponse<object>(null)
            {
                Message = "Query is required.",
                Success = false,
                StatusCode = 400
            });
        }

        try
        {
            var response = await cvSearchService.SearchAsync(request, cancellationToken);
            return Ok(new ApiResponse<CvSearchResponse>(response));
        }
        catch (AiParserException ex)
        {
            return StatusCode(ex.StatusCode, new
            {
                error = new
                {
                    message = ex.ResponseBody,
                    status = ex.StatusCode,
                    timestamp = DateTime.UtcNow.ToString("O"),
                    path = HttpContext.Request.Path.Value
                }
            });
        }
    }
}