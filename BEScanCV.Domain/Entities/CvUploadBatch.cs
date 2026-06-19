using System.ComponentModel.DataAnnotations.Schema;

namespace BEScanCV.Domain.Entities;

public sealed class CvUploadBatch
{
    public string Id { get; set; } = string.Empty;
    public long UploadedBy { get; set; }
    public string Status { get; set; } = "PENDING";
    public int TotalFiles { get; set; }
    public int CompletedFiles { get; set; }
    public int FailedFiles { get; set; }
    public int CancelledFiles { get; set; }
    public int ProcessingFiles { get; set; }
    public int PendingFiles { get; set; }
    public string RequestIds { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UploadedBy))]
    public User? Uploader { get; set; }
}
