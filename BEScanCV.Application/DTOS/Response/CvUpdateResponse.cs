using System.Text.Json;
using System.Text.Json.Serialization;

namespace BEScanCV.Application.DTOS.Response;

public sealed class CvUpdateResponse
{
    [JsonPropertyName("cv_info_id")]
    public long CvInfoId { get; set; }

    [JsonPropertyName("cv_file_id")]
    public long CvFileId { get; set; }

    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("educations")]
    public JsonElement? Educations { get; set; }

    [JsonPropertyName("certifications")]
    public string[] Certifications { get; set; } = [];

    [JsonPropertyName("work_experience")]
    public CvWorkExperienceDto[] WorkExperience { get; set; } = [];

    [JsonPropertyName("is_marked")]
    public bool IsMarked { get; set; }

    [JsonPropertyName("tag")]
    public string Tag { get; set; } = string.Empty;

    [JsonPropertyName("work_type")]
    public string? WorkType { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
