using System.Text.Json;
using System.Text.Json.Serialization;

namespace BEScanCV.Application.DTOS.Response;

public abstract class CvDataResponse
{
    [JsonPropertyName("cv_infos_id")]
    public long Id { get; set; }

    [JsonPropertyName("cv_file_id")]
    public long CvFileId { get; set; }

    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("position")]
    public string? Position { get; set; }

    [JsonPropertyName("total_experience_years")]
    public int? TotalExperienceYears { get; set; }

    [JsonPropertyName("date_of_birth")]
    public DateOnly? DateOfBirth { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("educations")]
    public JsonElement? Educations { get; set; }

    [JsonPropertyName("quality_score")]
    public double? QualityScore { get; set; }

    [JsonPropertyName("quality_reason")]
    public string? QualityReason { get; set; }

    [JsonPropertyName("quality_details")]
    public JsonElement? QualityDetails { get; set; }

    [JsonPropertyName("is_marked")]
    public bool IsMarked { get; set; }

    [JsonPropertyName("tag")]
    public string Tag { get; set; } = string.Empty;

    [JsonPropertyName("work_type")]
    public string? WorkType { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("cv_file")]
    public CvFileDataResponse? CvFile { get; set; }

    [JsonPropertyName("skills")]
    public string[] CvSkills { get; set; } = [];

    [JsonPropertyName("cv_certifications")]
    public CvCertificationDataResponse[] CvCertifications { get; set; } = [];

    [JsonPropertyName("work_experiences")]
    public CvWorkExperienceDataResponse[] WorkExperiences { get; set; } = [];

    [JsonPropertyName("scores")]
    public CvSemanticScoreResponse Scores { get; set; } = new();

    [JsonPropertyName("reasons")]
    public CvSemanticReasonsResponse Reasons { get; set; } = new();
}

public sealed class CvFileDataResponse
{
    [JsonPropertyName("cv_file_id")]
    public long Id { get; set; }

    [JsonPropertyName("uploaded_by")]
    public long UploadedBy { get; set; }

    [JsonPropertyName("original_file_name")]
    public string OriginalFileName { get; set; } = string.Empty;

    [JsonPropertyName("file_url")]
    public string FileUrl { get; set; } = string.Empty;

    [JsonPropertyName("file_type")]
    public string FileType { get; set; } = string.Empty;

    [JsonPropertyName("file_size")]
    public long FileSize { get; set; }

    [JsonPropertyName("ai_document_id")]
    public string? AiDocumentId { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

public sealed class CvSkillDataResponse
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("cv_info_id")]
    public long CvInfoId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public sealed class CvCertificationDataResponse
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("cv_info_id")]
    public long CvInfoId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public sealed class CvWorkExperienceDataResponse
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("cv_info_id")]
    public long CvInfoId { get; set; }

    [JsonPropertyName("company")]
    public string? Company { get; set; }

    [JsonPropertyName("position")]
    public string? Position { get; set; }

    [JsonPropertyName("duration")]
    public string? Duration { get; set; }

    [JsonPropertyName("responsibility")]
    public string? Responsibility { get; set; }
}
