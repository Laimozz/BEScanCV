using System.Text.Json.Serialization;

namespace BEScanCV.Application.DTOS.Requests;

public sealed class CvUpdateRequest
{
    [JsonPropertyName("full_name")]
    public string? FullName { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("educations")]
    public CvEducationUniversityUpdateRequest[]? Educations { get; set; }

    [JsonPropertyName("certifications")]
    public string[]? Certifications { get; set; }

    [JsonPropertyName("work_experience")]
    public CvWorkExperienceUpdateRequest[]? WorkExperience { get; set; }

    [JsonPropertyName("is_marked")]
    public bool? IsMarked { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }
}

public sealed class CvEducationUniversityUpdateRequest
{
    [JsonPropertyName("university")]
    public string? University { get; set; }
}

public sealed class CvWorkExperienceUpdateRequest
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }

    [JsonPropertyName("company")]
    public string? Company { get; set; }

    [JsonPropertyName("position")]
    public string? Position { get; set; }

    [JsonPropertyName("duration")]
    public string? Duration { get; set; }
}
