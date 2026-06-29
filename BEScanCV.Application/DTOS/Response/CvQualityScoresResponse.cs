using System.Text.Json.Serialization;

namespace BEScanCV.Application.DTOS.Response;

public sealed class CvQualityScoresResponse
{
    [JsonPropertyName("data")]
    public IReadOnlyCollection<CvQualityScoreResultResponse> Data { get; set; } = [];
}
