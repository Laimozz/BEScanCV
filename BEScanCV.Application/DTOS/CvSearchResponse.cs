using System.Text.Json.Serialization;

namespace BEScanCV.Application.DTOS;

public class PaginationMetaDto
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }

    public PaginationMetaDto(int total, int page, int limit, int totalPages)
    {
        Total = total;
        Page = page;
        Limit = limit;
        TotalPages = totalPages;
    }
}

public class CvSearchResponse
{
    [JsonPropertyName("items")]
    public CvSearchResultDto[] Items { get; set; }

    [JsonPropertyName("meta")]
    public PaginationMetaDto Meta { get; set; }

    public CvSearchResponse(CvSearchResultDto[] items, PaginationMetaDto meta)
    {
        Items = items;
        Meta = meta;
    }
}