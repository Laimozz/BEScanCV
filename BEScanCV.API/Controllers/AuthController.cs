using System.IdentityModel.Tokens.Jwt;
using BEScanCV.API.Common;
using BEScanCV.Application.DTOS;
using BEScanCV.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace BEScanCV.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IUserService userService, IJwtService jwtService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<CurrentUserWithTokenResponse>>> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct)
    {
        var user = await userService.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), ct);
        if (user == null || !userService.VerifyPassword(request.Password, user.PasswordHash))
            return Unauthorized(new ApiResponse<object>(null) { Success = false, Message = "Invalid username or password", StatusCode = 401 });
        var tokens = await jwtService.GenerateTokensAsync(user, ct);
        return Ok(new ApiResponse<CurrentUserWithTokenResponse>(tokens) { Message = "Login successful", StatusCode = 200 });
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<RefreshResponse>>> Refresh(
        [FromBody] RefreshRequest request,
        CancellationToken ct)
    {
        try
        {
            var tokens = await jwtService.RefreshTokenAsync(request.RefreshToken, ct);
            return Ok(new ApiResponse<RefreshResponse>(new RefreshResponse(tokens.AccessToken, tokens.AccessTokenExpiresAt)) { Message = "Token refreshed", StatusCode = 200 });
        }
        catch (SecurityTokenException)
        {
            return Unauthorized(new ApiResponse<object>(null) { Success = false, Message = "Invalid or expired refresh token", StatusCode = 401 });
        }
    }

    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<object>>> Logout(
        [FromBody] RefreshRequest request,
        CancellationToken ct)
    {
        await jwtService.RevokeRefreshTokenAsync(request.RefreshToken, ct);
        return Ok(new ApiResponse<object>(null) { Message = "Logged out successfully", StatusCode = 200 });
    }

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

    [HttpGet("me")]
    [Authorize] 
    public async Task<ActionResult<ApiResponse<CurrentUserResponse>>> GetCurrentUser(CancellationToken cancellationToken)
    {
        try
        {
            // Extract the user ID directly from the authenticated HttpContext claims
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            if (!long.TryParse(userIdClaim, out var userId))
                return Unauthorized(new ApiResponse<object>(null) { Success = false, Message = "Invalid token claims", StatusCode = 401 });

            var user = await userService.GetCurrentUserAsync(userId, cancellationToken);
            return Ok(new ApiResponse<CurrentUserResponse>(user));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ApiResponse<object>(null) { Success = false, Message = "User not found", StatusCode = 404 });
        }
    }

}