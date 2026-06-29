using System.Text.Json.Serialization;

namespace BEScanCV.Application.DTOS.Response;

public sealed class CvBatchUploadStatusResponse
{
    [JsonPropertyName("batch_id")]
    public string BatchId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("total_files")]
    public int TotalFiles { get; set; }

    [JsonPropertyName("completed_files")]
    public int CompletedFiles { get; set; }

    [JsonPropertyName("failed_files")]
    public int FailedFiles { get; set; }

    [JsonPropertyName("cancelled_files")]
    public int CancelledFiles { get; set; }

    [JsonPropertyName("processing_files")]
    public int ProcessingFiles { get; set; }

    [JsonPropertyName("pending_files")]
    public int PendingFiles { get; set; }

    [JsonPropertyName("progress")]
    public int Progress { get; set; }

    [JsonPropertyName("items")]
    public CvBatchUploadItemResponse[] Items { get; set; } = [];
}
