using System.Text.Json.Serialization;

namespace BEScanCV.Application.DTOS.Response;


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