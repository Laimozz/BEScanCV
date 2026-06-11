using System.ComponentModel.DataAnnotations.Schema;

namespace BEScanCV.Domain.Entities;

public sealed class CvSkill
{
    public long Id { get; set; }
    public long CvInfoId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal? YearsOfExperience { get; set; }

    [ForeignKey(nameof(CvInfoId))]
    public CvInfo? CvInfo { get; set; }
}
