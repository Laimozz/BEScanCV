using System.Net.Http.Json;
using System.Text.Json;
using BEScanCV.Application.DTOS;
using BEScanCV.Application.Exceptions;
using BEScanCV.Application.Interfaces;
using BEScanCV.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace BEScanCV.Infrastructure.Services;

public sealed class AiSemanticSearchClient(
    HttpClient httpClient,
    IOptions<AiServiceOptions> options) : ISemanticSearchClient
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly AiServiceOptions _options = options.Value;

    public async Task<IReadOnlyCollection<AiSemanticSearchResult>> SearchAsync(
        CvSemanticSearchRequest request,
        long userId,
        CancellationToken cancellationToken = default)
    {
        var aiRequest = new AiSemanticSearchRequest
        {
            Query = request.Query,
            TopK = request.TopK,
            UserId = userId
        };

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            _options.SemanticSearchPath)
        {
            Content = JsonContent.Create(aiRequest, options: JsonOptions)
        };

        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            httpRequest.Headers.Add("X-API-Key", _options.ApiKey);
        }

        using var response = await httpClient.SendAsync(
            httpRequest,
            cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new AiParserException(
                (int)response.StatusCode,
                ReadErrorMessage(responseBody));
        }

        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return [];
        }

        return JsonSerializer.Deserialize<AiSemanticSearchResult[]>(
                   responseBody,
                   JsonOptions)
               ?? [];
    }

    private static string ReadErrorMessage(string responseBody)
    {
        try
        {
            using var errorJson = JsonDocument.Parse(responseBody);
            if (errorJson.RootElement.TryGetProperty(
                    "detail",
                    out var detailElement))
            {
                return detailElement.GetString() ?? responseBody;
            }
        }
        catch (JsonException)
        {
            // Keep the original response when the AI error is not JSON.
        }

        return responseBody;
    }
}
