using System.IdentityModel.Tokens.Jwt;
using BEScanCV.API.Common;
using BEScanCV.Application.DTOS;
using BEScanCV.Application.DTOS.Requests;
using BEScanCV.Application.DTOS.Response;
using BEScanCV.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Asn1;

namespace BEScanCV.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IUserService userService, IJwtService jwtService, ILogger<AuthController> logger) : ControllerBase
{    

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<CurrentUserWithTokenResponse>>> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct)
    {
        var user = await userService.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), ct);
        if (user == null || !userService.VerifyPassword(request.Password, user.PasswordHash))
        {
            logger.LogWarning("Failed login attempt for {Email} at {Timestamp}", request.Email, DateTime.UtcNow);
            return Unauthorized(new ApiResponse<object>(null) { Success = false, Message = "Invalid username or password", StatusCode = 401 });
        }
        var tokens = await jwtService.GenerateTokensAsync(user, ct);
        SetRefreshTokenCookie(tokens.RefreshToken!, tokens.RefreshTokenExpiresAt!.Value);
        logger.LogInformation("Successful login for {Email} at {Timestamp}", request.Email, DateTime.UtcNow);
        return Ok(new ApiResponse<CurrentUserWithTokenResponse>(tokens) { Message = "Login successful", StatusCode = 200 });
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<RefreshResponse>>> Refresh(
        CancellationToken ct)
    {
        try
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(refreshToken))
                return Unauthorized(new ApiResponse<object>(null) { Success = false, Message = "Refresh token is invalid or expired", StatusCode = 401 });

            var tokens = await jwtService.RefreshTokenAsync(refreshToken, ct);
            SetRefreshTokenCookie(tokens.RefreshToken!, tokens.RefreshTokenExpiresAt!.Value);
            return Ok(new ApiResponse<RefreshResponse>(new RefreshResponse(tokens.AccessToken, tokens.AccessTokenExpiresAt)) { Message = "Token refreshed successfully", StatusCode = 200 });
        }
        catch (SecurityTokenException)
        {
            ClearRefreshTokenCookie();
            return Unauthorized(new ApiResponse<object>(null) { Success = false, Message = "Refresh token is invalid or expired", StatusCode = 401 });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> Logout(
        CancellationToken ct)
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (!string.IsNullOrEmpty(refreshToken))
            await jwtService.RevokeRefreshTokenAsync(refreshToken, ct);
        ClearRefreshTokenCookie();
        return Ok(new ApiResponse<object>(null) { Message = "Logged out successfully", StatusCode = 200 });
    }

    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (!long.TryParse(userIdClaim, out var userId))
                return Unauthorized(new ApiResponse<object>(null) { Success = false, Message = "Invalid token claims", StatusCode = 401 });

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
    public async Task<ActionResult<ApiResponse<UserDto>>> GetCurrentUser(CancellationToken cancellationToken)
    {
        try
        {
            // Extract the user ID directly from the authenticated HttpContext claims
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            if (!long.TryParse(userIdClaim, out var userId))
                return Unauthorized(new ApiResponse<object>(null) { Success = false, Message = "Invalid token claims", StatusCode = 401 });

            var user = await userService.GetCurrentUserAsync(userId, cancellationToken);
            return Ok(new ApiResponse<UserDto>(user));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ApiResponse<object>(null) { Success = false, Message = "User not found", StatusCode = 404 });
        }
    }

    [HttpPatch("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        try
        {

            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (!long.TryParse(userIdClaim, out var userId))
                return Unauthorized(new ApiResponse<object>(null) { Success = false, Message = "Invalid token claims", StatusCode = 401 });

            var user = await userService.UpdateProfileAsync(userId, request.FullName, cancellationToken);
            return Ok(new ApiResponse<UserDto>(user) { Message = "Profile updated successfully", StatusCode = 200 });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ApiResponse<object>(null) { Success = false, Message = "User not found", StatusCode = 404 });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<object>(null) { Success = false, Message = ex.Message, StatusCode = 400 });
        }
    }

    private void SetRefreshTokenCookie(string token, DateTime expiresAt)
    {
        Response.Cookies.Append("refreshToken", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/api/v1/auth/refresh",
            Expires = expiresAt.ToUniversalTime()
        });
    }

    private void ClearRefreshTokenCookie()
    {
        Response.Cookies.Append("refreshToken", string.Empty, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/api/v1/auth/refresh",
            Expires = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)
        });
    }

}