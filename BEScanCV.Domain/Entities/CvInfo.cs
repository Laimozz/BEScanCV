using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace BEScanCV.Domain.Entities;

public sealed class CvInfo
{
    public long Id { get; set; }
    public long CvFileId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Position { get; set; }
    public int? TotalExperienceYears { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public string? Summary { get; set; }
    public JsonDocument? Educations { get; set; }
    public string? RawText { get; set; }
    public JsonDocument? ProfileData { get; set; }
    public double? QualityScore { get; set; }
    public string? QualityReason { get; set; }
    public JsonDocument? QualityDetails { get; set; }
    public bool IsMarked { get; set; }
    public string Tag { get; set; } = "new";
    public string? WorkType { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(CvFileId))]
    public CvFile? CvFile { get; set; }

    [InverseProperty(nameof(CvSkill.CvInfo))]
    public ICollection<CvSkill> CvSkills { get; set; } = [];

    [InverseProperty(nameof(CvCertification.CvInfo))]
    public ICollection<CvCertification> CvCertifications { get; set; } = [];

    [InverseProperty(nameof(CvWorkExperience.CvInfo))]
    public ICollection<CvWorkExperience> WorkExperiences { get; set; } = [];
}
