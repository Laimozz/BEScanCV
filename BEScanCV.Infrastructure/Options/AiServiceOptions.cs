namespace BEScanCV.Infrastructure.Options;

public sealed class AiServiceOptions
{
    public const string SectionName = "AiService";

    public string BaseUrl { get; set; } = string.Empty;
    public string ParseSearchQueryPath { get; set; } = "/parse-search-query";
    public string? ApiKey { get; set; }
}
