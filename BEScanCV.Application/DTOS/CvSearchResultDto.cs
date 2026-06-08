using System.Text.Json.Serialization;

namespace BEScanCV.Application.DTOS;

public sealed record CvSearchResultDto(
    [property: JsonPropertyName("FullName")] string FullName,
    [property: JsonPropertyName("Email")] string Email,
    [property: JsonPropertyName("Skill")] IReadOnlyCollection<string> Skill,
    [property: JsonPropertyName("Created_at")] DateTime CreatedAt,
    [property: JsonPropertyName("Uploaded_by")] long UploadedBy);
