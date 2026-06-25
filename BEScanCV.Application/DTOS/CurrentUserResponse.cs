using System.Text.Json.Serialization;

namespace BEScanCV.Application.DTOS;

public sealed record CurrentUserResponse
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("lastActive")]
    public DateTime LastActive { get; set; }
}
