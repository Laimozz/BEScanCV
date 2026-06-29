using System.Text.Json.Serialization;

namespace BEScanCV.Application.DTOS.Response;

public sealed class CvQualityScoreResultResponse
{
    [JsonPropertyName("cv_id")]
    public string CvId { get; set; } = string.Empty;

    [JsonPropertyName("quality_score")]
    public double? QualityScore { get; set; }

    [JsonPropertyName("quality_reason")]
    public string? QualityReason { get; set; }
}
