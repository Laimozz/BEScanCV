namespace BEScanCV.Application.DTOS;

public sealed record TokenResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);