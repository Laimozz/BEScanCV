using System.Text.Json.Serialization;

namespace BEScanCV.Application.DTOS;

public class CvSearchResultDto
{
    [JsonPropertyName("full_name")]
    public string FullName { get; set; }

    [JsonPropertyName("email")]

    public string Email { get; set; }

    [JsonPropertyName("cv_file_id")]
    public long CvFileId { get; set; }
    
    [JsonPropertyName("skills")]
    public string[] Skills { get; set; }
    
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [JsonPropertyName("uploaded_by")]
    public CvUploaderDto UploadedBy { get; set; }

    public CvSearchResultDto(
        string fullName,
        string email,
        long cvFileId,
        string[] skills,
        DateTime createdAt,
        CvUploaderDto uploadedBy)
    {
        FullName = fullName;
        Email = email;
        CvFileId = cvFileId;
        Skills = skills;
        CreatedAt = createdAt;
        UploadedBy = uploadedBy;
    }
}
