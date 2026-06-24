using System.Text.Json.Serialization;

namespace BEScanCV.Application.DTOS;

public sealed record RefreshResponse()
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = default!;

    [JsonPropertyName("accessTokenExpiresAt")]
    public DateTime AccessTokenExpiresAt { get; set; }

    public RefreshResponse(string token, DateTime expiresAt) : this()
    {
        AccessToken = token;
        AccessTokenExpiresAt = expiresAt;
    }
}