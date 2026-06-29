using BEScanCV.API.Common;
using BEScanCV.Application.DTOS.Requests;
using BEScanCV.Application.DTOS.Response;
using BEScanCV.Application.Exceptions;
using BEScanCV.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BEScanCV.API.Controllers;

[ApiController]
[Route("api/v1/cvs/getAll")]
//[Authorize]
public sealed class CvGetAllController(ICvGetAllService cvGetAllService) : ControllerBase
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
