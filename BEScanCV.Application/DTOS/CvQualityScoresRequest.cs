using System.Text.Json.Serialization;

namespace BEScanCV.Application.DTOS;

public sealed class CvQualityScoresRequest
{
    [JsonPropertyName("cv_ids")]
    public string[] CvIds { get; set; } = [];
}
