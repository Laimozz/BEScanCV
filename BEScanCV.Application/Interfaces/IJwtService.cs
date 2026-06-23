using BEScanCV.Application.DTOS;
using BEScanCV.Domain.Entities;

namespace BEScanCV.Application.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    Task<CurrentUserWithTokenResponse> GenerateTokensAsync(User user, CancellationToken ct = default);
    long GetUserIdFromToken(string accessToken);
    Task<CurrentUserWithTokenResponse> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    string GenerateRawRefreshToken();
}