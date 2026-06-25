using System.Text.Json.Serialization;

namespace BEScanCV.Application.DTOS;

/// <summary>
/// Reuse CvDataResponse khi mark (trả full object).
/// Khi unmark chỉ cần <see cref="UnmarkTalentResponse"/> gọn hơn.
/// </summary>
public sealed class MarkTalentResponse : CvDataResponse;

public sealed class UnmarkTalentResponse
{
    [JsonPropertyName("cv_infos_id")]
    public long CvInfosId { get; set; }

    [JsonPropertyName("is_marked")]
    public bool IsMarked { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
