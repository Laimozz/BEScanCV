using BEScanCV.Application.DTOS;
using BEScanCV.Domain.Entities;

namespace BEScanCV.Application.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    Task<TokenResponse> GenerateTokensAsync(User user, CancellationToken ct = default);
    Task<TokenResponse> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken ct = default);
}