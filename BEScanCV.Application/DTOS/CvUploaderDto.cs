using System.Text.Json.Serialization;

namespace BEScanCV.Application.DTOS;

public class CvUploaderDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("full_name")]
    public string FullName { get; set; }

    public CvUploaderDto(long id, string fullName)
    {
        Id = id;
        FullName = fullName;
    }
}
