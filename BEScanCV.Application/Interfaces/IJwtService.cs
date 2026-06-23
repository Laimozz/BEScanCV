using BEScanCV.Application.DTOS;
using BEScanCV.Domain.Entities;

namespace BEScanCV.Application.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    Task<CurrentUserResponse> GenerateTokensAsync(User user, CancellationToken ct = default);
    Task<CurrentUserResponse> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken ct = default);
}