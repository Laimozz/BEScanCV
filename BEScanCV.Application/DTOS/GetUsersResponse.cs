using System.Text.Json.Serialization;

namespace BEScanCV.Application.DTOS;

public class GetUsersResponse
{
    [JsonPropertyName("items")]
    public IReadOnlyList<UserItemDto> Items { get; set; } = [];

    [JsonPropertyName("pagination")]
    public UserPaginationDto Pagination { get; set; } = null!;
}

public class UserPaginationDto
{
    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("totalItems")]
    public int TotalItems { get; set; }

    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }
}
