using System.Text.Json.Serialization;

namespace BEScanCV.Application.DTOS;

public class CvGetAllResponse
{
    [JsonPropertyName("items")]
    public CvSearchResultDto[] Items { get; set; }

    [JsonPropertyName("meta")]
    public PaginationMetaDto Meta { get; set; }

    public CvGetAllResponse(CvSearchResultDto[] items, PaginationMetaDto meta)
    {
        Items = items;
        Meta = meta;
    }
}