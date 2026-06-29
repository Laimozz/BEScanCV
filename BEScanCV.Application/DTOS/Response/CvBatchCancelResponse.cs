using System.Text.Json.Serialization;

namespace BEScanCV.Application.DTOS.Response;

public sealed class CvBatchCancelResponse
{
    [JsonPropertyName("batch_id")]
    public string BatchId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

