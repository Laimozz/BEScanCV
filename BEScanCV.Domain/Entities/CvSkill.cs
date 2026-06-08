using System.ComponentModel.DataAnnotations.Schema;

namespace BEScanCV.Domain.Entities;

public sealed class CvSkill
{
    public long CvInfoId { get; set; }
    public long SkillId { get; set; }
    public decimal? ConfidenceScore { get; set; }
    public decimal? YearsOfExperience { get; set; }

    [ForeignKey(nameof(CvInfoId))]
    public CvInfo? CvInfo { get; set; }

    [ForeignKey(nameof(SkillId))]
    public Skill? Skill { get; set; }
}
