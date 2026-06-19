using System.Text.Json.Serialization;

namespace BEScanCV.Application.DTOS;

public class UpdateUserRequest
{
    [JsonPropertyName("fullName")]
    public string? FullName { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}
