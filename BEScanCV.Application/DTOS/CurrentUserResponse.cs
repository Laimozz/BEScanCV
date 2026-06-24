using System.Text.Json.Serialization;

namespace BEScanCV.Application.DTOS;

public sealed record CurrentUserResponse()
{

    [JsonPropertyName("user")]
    public UserDto User { get; set; } = default!;
}