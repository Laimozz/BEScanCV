using System.Text.Json.Serialization;

namespace BEScanCV.Application.DTOS.Response;

public class CreateUserResponse
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
}
