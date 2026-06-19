namespace BEScanCV.Infrastructure.Options;

public sealed class AiServiceOptions
{
    public const string SectionName = "AiService";

    public string BaseUrl { get; set; } = string.Empty;
    public string ParseSearchQueryPath { get; set; } = "/parse-search-query";
    public string ProcessCvPath { get; set; } = "/api/v1/cv/index";
    public string? ApiKey { get; set; }
}
