using System.Text.Json.Serialization;

namespace BEScanCV.Application.DTOS;

public sealed class UpdateProfileRequest
{
    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;


}