using System.Text.Json.Serialization;

namespace BEScanCV.Application.DTOS;

public sealed class CvQualityScoresRequest
{
    [JsonPropertyName("user_id")]
    public long UserId { get; set; }

    [JsonPropertyName("cv_ids")]
    public string[] CvIds { get; set; } = [];
}
