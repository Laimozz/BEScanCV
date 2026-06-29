using System.Text.Json.Serialization;

namespace BEScanCV.Application.DTOS.Response;

public class CvGetAllResponse
{
    [JsonPropertyName("items")]
    public CvGetAllItemResponse[] Items { get; set; }

    [JsonPropertyName("meta")]
    public PaginationMetaDto Meta { get; set; }

    public CvGetAllResponse(
        CvGetAllItemResponse[] items,
        PaginationMetaDto meta)
    {
        Items = items;
        Meta = meta;
    }
}

public sealed class CvGetAllItemResponse : CvDataResponse;
