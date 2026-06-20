using System.Text.Json.Serialization;

namespace BEScanCV.Application.DTOS;

public sealed class CvBulkUploadResponse
{
    [JsonPropertyName("batch_id")]
    public string BatchId { get; set; } = string.Empty;

    [JsonPropertyName("accepted_files")]
    public int AcceptedFiles { get; set; }

    [JsonPropertyName("total_accepted_files")]
    public int TotalAcceptedFiles { get; set; }

    [JsonPropertyName("websocket_endpoint")]
    public string WebsocketEndpoint { get; set; } = string.Empty;
}
