using System.Text.Json;
using System.Text.Json.Serialization;

namespace BEScanCV.Application.DTOS.Requests;

public sealed class CvQualityScoreRequest
{
    [JsonPropertyName("cv_id")]
    public string CvId { get; set; } = string.Empty;

    [JsonPropertyName("quality_score")]
    public double? QualityScore { get; set; }

    [JsonPropertyName("quality_reason")]
    public string? QualityReason { get; set; }

    [JsonPropertyName("quality_details")]
    public JsonElement? QualityDetails { get; set; }
}
