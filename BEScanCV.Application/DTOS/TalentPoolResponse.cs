using System.Text.Json.Serialization;

namespace BEScanCV.Application.DTOS;

public sealed class TalentPoolResponse
{
    [JsonPropertyName("items")]
    public TalentPoolItemResponse[] Items { get; set; }

    [JsonPropertyName("meta")]
    public PaginationMetaDto Meta { get; set; }

    public TalentPoolResponse(TalentPoolItemResponse[] items, PaginationMetaDto meta)
    {
        Items = items;
        Meta = meta;
    }
}

/// <summary>Reuse CvDataResponse — talent pool item giống hệt CvItem trong API doc.</summary>
public sealed class TalentPoolItemResponse : CvDataResponse;
