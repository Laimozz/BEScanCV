using System.Text.Json.Serialization;

namespace BEScanCV.Application.DTOS.Requests;

public sealed class MarkTalentRequest
{
    [JsonPropertyName("is_marked")]
    public bool IsMarked { get; set; }
}
