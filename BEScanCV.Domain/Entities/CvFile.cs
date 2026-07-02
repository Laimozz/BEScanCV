using System.ComponentModel.DataAnnotations.Schema;

namespace BEScanCV.Domain.Entities;

public sealed class CvFile
{
    public long Id { get; set; }
    public long UploadedBy { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? AiDocumentId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public long? DeletedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UploadedBy))]
    public User? Uploader { get; set; }

    [InverseProperty(nameof(CvInfo.CvFile))]
    public CvInfo? CvInfo { get; set; }
}
