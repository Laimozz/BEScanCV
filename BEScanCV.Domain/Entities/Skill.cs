using System.ComponentModel.DataAnnotations.Schema;

namespace BEScanCV.Domain.Entities;

public sealed class Skill
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [InverseProperty(nameof(CvSkill.Skill))]
    public ICollection<CvSkill> CvSkills { get; set; } = [];
}
