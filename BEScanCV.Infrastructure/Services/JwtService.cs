using System.Security.Cryptography;
using BEScanCV.Application.DTOS;
using BEScanCV.Application.Interfaces;
using BEScanCV.Application.Interfaces.Repositories;
using BEScanCV.Domain.Entities;
using BEScanCV.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BEScanCV.Infrastructure.Services;

public sealed class JwtService(
    IOptions<JwtOptions> jwtOptions,
    IRefreshTokenRepository refreshTokenRepo,
    IHasher hasher) : IJwtService
{
    private readonly JwtOptions _options = jwtOptions.Value;

    public string GenerateAccessToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("status", user.Status)
        };

        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_options.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.AccessTokenExpiryMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<CurrentUserWithTokenResponse> GenerateTokensAsync(User user, CancellationToken ct = default)
    {
        var accessToken = GenerateAccessToken(user);
        var refreshToken = GenerateRawRefreshToken();
        var refreshExpiry = DateTime.UtcNow.AddDays(_options.RefreshTokenExpiryDays);
        var accessTokenExpiry = DateTime.UtcNow.AddMinutes(_options.AccessTokenExpiryMinutes);

        var refreshEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = hasher.Hash(refreshToken),
            ExpiresAt = refreshExpiry,
            CreatedAt = DateTime.UtcNow
        };

        await refreshTokenRepo.AddAsync(refreshEntity, ct);

        return new CurrentUserWithTokenResponse
        {
            AccessToken = accessToken,
            AccessTokenExpiresAt = accessTokenExpiry,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = refreshExpiry,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                Status = user.Status,
                LastActive = user.LastActive
            }
        };
    }

    public long GetUserIdFromToken(string accessToken)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(accessToken);
        var userIdClaim = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);

        if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
            throw new SecurityTokenException("Invalid token");

        return userId;
    }

    public async Task<CurrentUserWithTokenResponse> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var tokenHash = hasher.Hash(refreshToken);
        var stored = await refreshTokenRepo.GetByTokenHashAsync(tokenHash, ct);

        if (stored == null || stored.RevokedAt != null || stored.ExpiresAt < DateTime.UtcNow)
            throw new SecurityTokenException("Invalid or expired refresh token");

        await refreshTokenRepo.RevokeAsync(stored, ct);
        var newTokens = await GenerateTokensAsync(stored.User!, ct);
        return new CurrentUserWithTokenResponse
        {
            AccessToken = newTokens.AccessToken,
            AccessTokenExpiresAt = newTokens.AccessTokenExpiresAt,
            RefreshToken = newTokens.RefreshToken,
            RefreshTokenExpiresAt = newTokens.RefreshTokenExpiresAt,
            User = newTokens.User
        };
    }

    public string GenerateRawRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var tokenHash = hasher.Hash(refreshToken);
        var stored = await refreshTokenRepo.GetByTokenHashAsync(tokenHash, ct);

        if (stored != null && stored.RevokedAt == null)
            await refreshTokenRepo.RevokeAsync(stored, ct);
    }
}