using BEScanCV.Application.DTOS;
using BEScanCV.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BEScanCV.API.Controllers;

[ApiController]
[Route("api/v1/cvs/search")]
public sealed class CvSearchController(ICvSearchService cvSearchService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CvSearchResponse>> Search(
        [FromBody] CvSearchRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest("Query is required.");
        }

        var response = await cvSearchService.SearchAsync(request, cancellationToken);
        return Ok(response);
    }
}
