using System.Text.Json.Serialization;

namespace BEScanCV.Application.DTOS;

public sealed record CurrentUserWithTokenResponse()
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = default!;

    [JsonPropertyName("accessTokenExpiresAt")]
    public DateTime AccessTokenExpiresAt { get; set; }

    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = default!;

    [JsonPropertyName("refreshTokenExpiresAt")]

    public DateTime RefreshTokenExpiresAt { get; set; } 

    [JsonPropertyName("user")]
    public UserDto User { get; set; } = default!;
}