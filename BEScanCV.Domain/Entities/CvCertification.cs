using System.ComponentModel.DataAnnotations.Schema;

namespace BEScanCV.Domain.Entities;

public sealed class CvCertification
{
    public long Id { get; set; }
    public long CvInfoId { get; set; }
    public string Name { get; set; } = string.Empty;

    [ForeignKey(nameof(CvInfoId))]
    public CvInfo? CvInfo { get; set; }
}
