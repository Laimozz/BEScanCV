using System.Text.Json.Serialization;

namespace BEScanCV.Application.DTOS;

public sealed class CvSearchSemanticResponse : CvDataResponse
{
    [JsonPropertyName("score")]
    public CvSemanticScoreResponse Score { get; set; } = new();

    [JsonPropertyName("reasons")]
    public CvSemanticReasonsResponse Reasons { get; set; } = new();
}

public sealed class CvSemanticScoreResponse
{
    [JsonPropertyName("offline_score")]
    public double OfflineScore { get; set; }

    [JsonPropertyName("matching_score")]
    public double MatchingScore { get; set; }

    [JsonPropertyName("final_score")]
    public double FinalScore { get; set; }
}

public sealed class CvSemanticReasonsResponse
{
    [JsonPropertyName("offline_reason")]
    public string? OfflineReason { get; set; }

    [JsonPropertyName("matching_reason")]
    public string? MatchingReason { get; set; }

    [JsonPropertyName("overall_conclusion")]
    public string? OverallConclusion { get; set; }
}

public sealed class AiSemanticSearchResult
{
    [JsonPropertyName("cv_id")]
    public string CvId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("job_position")]
    public string? JobPosition { get; set; }

    [JsonPropertyName("scores")]
    public CvSemanticScoreResponse Scores { get; set; } = new();

    [JsonPropertyName("reasons")]
    public CvSemanticReasonsResponse Reasons { get; set; } = new();
}
