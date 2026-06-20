using BEScanCV.API.Common;
using BEScanCV.Application.DTOS;
using BEScanCV.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace BEScanCV.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IUserService userService, IJwtService jwtService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<TokenResponse>>> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct)
    {
        var user = await userService.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), ct);
        if (user == null || !userService.VerifyPassword(request.Password, user.PasswordHash))
            return Unauthorized(new ApiResponse<object>(null) { Success = false, Message = "Invalid credentials", StatusCode = 401 });
        var tokens = await jwtService.GenerateTokensAsync(user, ct);
        return Ok(new ApiResponse<TokenResponse>(tokens) { Message = "Login successful" });
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<TokenResponse>>> Refresh(
        [FromBody] RefreshRequest request,
        CancellationToken ct)
    {
        try
        {
            var tokens = await jwtService.RefreshTokenAsync(request.RefreshToken, ct);
            return Ok(new ApiResponse<TokenResponse>(tokens) { Message = "Token refreshed" });
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
        return Ok(new ApiResponse<object>(null) { Message = "Logged out successfully" });
    }
}