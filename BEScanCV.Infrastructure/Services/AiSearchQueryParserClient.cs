// BEScanCV.Infrastructure/Services/AiSearchQueryParserClient.cs
using System.Net.Http.Json;
using System.Text.Json;
using BEScanCV.Application.DTOS;
using BEScanCV.Application.Exceptions;
using BEScanCV.Application.Interfaces;
using BEScanCV.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace BEScanCV.Infrastructure.Services;

public sealed class AiSearchQueryParserClient(HttpClient httpClient, IOptions<AiServiceOptions> options) : ISearchQueryParser
{
    private readonly AiServiceOptions _options = options.Value;

    public async Task<CvSearchCriteriaDto> ParseAsync(string query, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _options.ParseSearchQueryPath)
        {
            Content = JsonContent.Create(new { text = query })
        };

        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            request.Headers.Add("X-API-Key", _options.ApiKey);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var errorMessage = responseBody;

            // Cố gắng parse JSON để lấy field "detail"
            try
            {
                var errorJson = JsonDocument.Parse(responseBody);
                if (errorJson.RootElement.TryGetProperty("detail", out var detailElement))
                {
                    errorMessage = detailElement.GetString() ?? responseBody;
                }
            }
            catch (JsonException)
            {
                // Nếu chuỗi trả về không phải định dạng JSON (VD: lỗi 500 HTML), thì sẽ giữ nguyên gốc responseBody
            }

            throw new AiParserException((int)response.StatusCode, errorMessage);
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await ReadCriteriaAsync(responseStream, cancellationToken);
    }

    private static async Task<CvSearchCriteriaDto> ReadCriteriaAsync(Stream responseStream, CancellationToken cancellationToken)
    {
        var json = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);
        var root = json.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
        {
            return CvSearchCriteriaDto.Empty;
        }

        var fields = new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in root.EnumerateObject())
        {
            AddField(fields, property.Name, property.Value);
        }

        return new CvSearchCriteriaDto(fields);
    }

    private static void AddField(IDictionary<string, IReadOnlyCollection<string>> fields, string name, JsonElement value)
    {
        if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return;
        }

        if (value.ValueKind == JsonValueKind.Array)
        {
            var values = value.EnumerateArray()
                .Select(ToSearchValue)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item!)
                .SelectMany(SplitSearchValues)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (values.Length > 0)
            {
                fields[name] = values;
            }
            return;
        }

        var searchValue = ToSearchValue(value);
        if (!string.IsNullOrWhiteSpace(searchValue))
        {
            fields[name] = SplitSearchValues(searchValue)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    private static IEnumerable<string> SplitSearchValues(string value)
    {
        return value
            .Split([',', ';', '|', '/'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(item => !string.IsNullOrWhiteSpace(item));
    }

    private static string? ToSearchValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }
}