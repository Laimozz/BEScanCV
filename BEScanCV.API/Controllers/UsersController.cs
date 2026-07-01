using BEScanCV.API.Common;
using BEScanCV.Application.DTOS.Requests;
using BEScanCV.Application.DTOS.Response;
using BEScanCV.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Microsoft.Extensions.Logging;

namespace BEScanCV.API.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize]
public sealed class UsersController(IUserService userService, ILogger<UsersController> logger) : ControllerBase
{
    /// <summary>
    /// GET /api/v1/users?page=1&limit=10&role=&status=
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<GetUsersResponse>>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10,
        [FromQuery] string? role = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Retrieving users. Page: {Page}, Limit: {Limit}, Role: {Role}, Status: {Status} at {Timestamp}", page, limit, role ?? "all", status ?? "all", DateTime.UtcNow);

        var response = await userService.GetUsersAsync(page, limit, role, status, cancellationToken);

        logger.LogInformation("Retrieved {Count} users (Page: {Page}, Limit: {Limit}) at {Timestamp}", response.Meta.Total, page, limit, DateTime.UtcNow);
        return Ok(new ApiResponse<GetUsersResponse>(response)
        {
            Message = "Users retrieved successfully"
        });
    }
    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<GetUserResponse>>> GetUserById(
        long id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Retrieving user with ID {UserId} at {Timestamp}", id, DateTime.UtcNow);

            var response = await userService.GetUserByIdAsync(id, cancellationToken);
            if (response is null)
            {
                logger.LogWarning("User with ID {UserId} not found at {Timestamp}", id, DateTime.UtcNow);
                return NotFound(new ApiResponse<GetUserResponse>(null)
                {
                    Message = "User not found",
                    Success = false,
                    StatusCode = StatusCodes.Status404NotFound
                });
            }
            logger.LogInformation("Retrieved user with ID {UserId} at {Timestamp}", id, DateTime.UtcNow);
            return Ok(new ApiResponse<GetUserResponse>(response)
            {
                Message = "User retrieved successfully"
            });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Failed to retrieve user with ID {UserId} at {Timestamp}: {Message}", id, DateTime.UtcNow, ex.Message);
            return BadRequest(new ApiResponse<object>(null)
            {
                Success = false,
                Message = ex.Message,
                StatusCode = 400
            });
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Failed to retrieve user with ID {UserId} at {Timestamp}: {Message}", id, DateTime.UtcNow, ex.Message);
            return BadRequest(new ApiResponse<object>(null)
            {
                Success = false,
                Message = ex.Message,
                StatusCode = 400
            });
        }
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
            logger.LogInformation("Creating user with email {Email} at {Timestamp}", request.Email, DateTime.UtcNow);

            var response = await userService.CreateUserAsync(request, cancellationToken);

            logger.LogInformation("Created user with ID {UserId} at {Timestamp}", response.Id, DateTime.UtcNow);
            return StatusCode(StatusCodes.Status201Created, new ApiResponse<CreateUserResponse>(response)
            {
                Message = "User created successfully",
                StatusCode = 201
            });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Failed to create user at {Timestamp}: {Message}", DateTime.UtcNow, ex.Message);
            return BadRequest(new ApiResponse<object>(null)
            {
                Success = false,
                Message = ex.Message,
                StatusCode = 400
            });
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Failed to create user at {Timestamp}: {Message}", DateTime.UtcNow, ex.Message);
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
            logger.LogInformation("Updating user with ID {UserId} at {Timestamp}", id, DateTime.UtcNow);

            await userService.UpdateUserAsync(id, request, cancellationToken);

            logger.LogInformation("Updated user with ID {UserId} at {Timestamp}", id, DateTime.UtcNow);
            return Ok(new ApiResponse<object>(null)
            {
                Message = "User updated successfully"
            });
        }
        catch (KeyNotFoundException)
        {
            logger.LogWarning("User with ID {UserId} not found for update at {Timestamp}", id, DateTime.UtcNow);
            return NotFound(new ApiResponse<object>(null)
            {
                Success = false,
                Message = "User not found",
                StatusCode = 404
            });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Failed to update user with ID {UserId} at {Timestamp}: {Message}", id, DateTime.UtcNow, ex.Message);
            return BadRequest(new ApiResponse<object>(null)
            {
                Success = false,
                Message = ex.Message,
                StatusCode = 400
            });
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Failed to update user with ID {UserId} at {Timestamp}: {Message}", id, DateTime.UtcNow, ex.Message);
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
            logger.LogInformation("Deleting user with ID {UserId} at {Timestamp}", id, DateTime.UtcNow);

            await userService.DeleteUserAsync(id, cancellationToken);

            logger.LogInformation("Deleted user with ID {UserId} at {Timestamp}", id, DateTime.UtcNow);
            return Ok(new ApiResponse<object>(null)
            {
                Message = "User deleted successfully"
            });
        }
        catch (KeyNotFoundException)
        {
            logger.LogWarning("User with ID {UserId} not found for deletion at {Timestamp}", id, DateTime.UtcNow);
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
   
}
