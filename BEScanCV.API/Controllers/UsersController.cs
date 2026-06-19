using BEScanCV.API.Common;
using BEScanCV.Application.DTOS;
using BEScanCV.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BEScanCV.API.Controllers;

[ApiController]
[Route("api/v1/users")]
public sealed class UsersController(IUserService userService) : ControllerBase
{
    /// <summary>
    /// GET /api/v1/users?page=1&pageSize=10&role=&status=
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<GetUsersResponse>>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? role = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var response = await userService.GetUsersAsync(page, pageSize, role, status, cancellationToken);
        return Ok(new ApiResponse<GetUsersResponse>(response)
        {
            Message = "Users retrieved successfully"
        });
    }

    /// <summary>
    /// POST /api/v1/users
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CreateUserResponse>>> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await userService.CreateUserAsync(request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, new ApiResponse<CreateUserResponse>(response)
            {
                Message = "User created successfully",
                StatusCode = 201
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<object>(null)
            {
                Success = false,
                Message = ex.Message,
                StatusCode = 400
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<object>(null)
            {
                Success = false,
                Message = ex.Message,
                StatusCode = 400
            });
        }
    }

    /// <summary>
    /// PATCH /api/v1/users/{id}
    /// </summary>
    [HttpPatch("{id:long}")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateUser(
        long id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await userService.UpdateUserAsync(id, request, cancellationToken);
            return Ok(new ApiResponse<object>(null)
            {
                Message = "User updated successfully"
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ApiResponse<object>(null)
            {
                Success = false,
                Message = "User not found",
                StatusCode = 404
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<object>(null)
            {
                Success = false,
                Message = ex.Message,
                StatusCode = 400
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<object>(null)
            {
                Success = false,
                Message = ex.Message,
                StatusCode = 400
            });
        }
    }

    /// <summary>
    /// DELETE /api/v1/users/{id}
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteUser(
        long id,
        CancellationToken cancellationToken)
    {
        try
        {
            await userService.DeleteUserAsync(id, cancellationToken);
            return Ok(new ApiResponse<object>(null)
            {
                Message = "User deleted successfully"
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ApiResponse<object>(null)
            {
                Success = false,
                Message = "User not found",
                StatusCode = 404
            });
        }
    }

    /// <summary>
    /// PATCH /api/v1/users/change-password/{userId}
    /// </summary>
    [HttpPatch("change-password/{userId:long}")]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword(
        long userId,
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await userService.ChangePasswordAsync(userId, request, cancellationToken);
            return Ok(new ApiResponse<object>(null)
            {
                Message = "Password changed successfully"
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ApiResponse<object>(null)
            {
                Success = false,
                Message = "User not found",
                StatusCode = 404
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<object>(null)
            {
                Success = false,
                Message = ex.Message,
                StatusCode = 400
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<object>(null)
            {
                Success = false,
                Message = ex.Message,
                StatusCode = 400
            });
        }
    }
}
