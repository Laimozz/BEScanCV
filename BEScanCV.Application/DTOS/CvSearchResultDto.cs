using System.Text.Json.Serialization;

namespace BEScanCV.Application.DTOS;

public class CvSearchResultDto
{
    public string FullName { get; set; }
    public string Email { get; set; }
    
    [JsonPropertyName("skill")]
    public string[] Skill { get; set; }
    
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [JsonPropertyName("uploaded_by")]
    public long UploadedBy { get; set; }

    public CvSearchResultDto(string fullName, string email, string[] skill, DateTime createdAt, long uploadedBy)
    {
        FullName = fullName;
        Email = email;
        Skill = skill;
        CreatedAt = createdAt;
        UploadedBy = uploadedBy;
    }
}