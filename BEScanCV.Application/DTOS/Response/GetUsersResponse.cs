using System.Text.Json.Serialization;

namespace BEScanCV.Application.DTOS.Response;

public class GetUsersResponse
{
    [JsonPropertyName("items")]
    public IReadOnlyList<UserItemDto> Items { get; set; } = [];

    [JsonPropertyName("meta")]
    public PaginationMetaDto Meta { get; set; } = null!;
}
