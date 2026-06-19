using System.Text.Json.Serialization;

namespace BEScanCV.Application.DTOS;

public sealed class CvWorkExperienceDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("company")]
    public string? Company { get; set; }

    [JsonPropertyName("position")]
    public string? Position { get; set; }

    [JsonPropertyName("duration")]
    public string? Duration { get; set; }

    [JsonPropertyName("responsibility")]
    public string? Responsibility { get; set; }
}
