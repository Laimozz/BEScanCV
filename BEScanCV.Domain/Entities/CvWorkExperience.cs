using System.ComponentModel.DataAnnotations.Schema;

namespace BEScanCV.Domain.Entities;

public sealed class CvWorkExperience
{
    public long Id { get; set; }
    public long CvInfoId { get; set; }
    public string? Company { get; set; }
    public string? Position { get; set; }
    public string? Duration { get; set; }
    public string? Responsibility { get; set; }

    [ForeignKey(nameof(CvInfoId))]
    public CvInfo? CvInfo { get; set; }
}
