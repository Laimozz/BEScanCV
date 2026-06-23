using System.Text.Json.Serialization;

namespace BEScanCV.Application.DTOS;

public sealed record CurrentUserWithTokenResponse()
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = default!;

    [JsonPropertyName("accessTokenExpiresAt")]
    public DateTime AccessTokenExpiresAt { get; set; }

    [JsonPropertyName("user")]
    public UserDto User { get; set; } = default!;
}