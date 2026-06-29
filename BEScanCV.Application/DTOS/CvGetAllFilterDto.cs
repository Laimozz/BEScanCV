using System.Text.Json.Serialization;

namespace BEScanCV.Application.DTOS;

public class CvGetAllFilterDto
{
    [JsonPropertyName("total_experience_years")]
    public int? TotalExperienceYears  { get; set; }
    [JsonPropertyName("skills")]
    public string? Skills { get; set; }
    [JsonPropertyName("position")]
    public string? Position { get; set; }
    [JsonPropertyName("location")]
    public string? Location { get; set; }
    [JsonPropertyName("work_type")]
    public string? WorkType { get; set; }
}
