using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BEScanCV.Application.DTOS.Requests;
using BEScanCV.Application.DTOS.Response;
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
        CancellationToken cancellationToken = default)
    {
        var aiRequest = new AiSemanticSearchRequest
        {
            Query = request.Query,
            TopK = request.TopK
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

        HttpResponseMessage response;
        string responseBody;

        try
        {
            response = await httpClient.SendAsync(
                httpRequest,
                cancellationToken);
            responseBody = await response.Content.ReadAsStringAsync(
                cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AiParserException(
                (int)HttpStatusCode.GatewayTimeout,
                $"AI semantic search request timed out or connection was aborted after {httpClient.Timeout.TotalSeconds:0} seconds.");
        }
        catch (HttpRequestException ex)
        {
            throw new AiParserException(
                (int)HttpStatusCode.BadGateway,
                $"Cannot connect to AI semantic search service. {ex.Message}");
        }

        using (response)
        {
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

            try
            {
                var result = JsonSerializer.Deserialize<AiSemanticSearchResponse>(
                    responseBody,
                    JsonOptions);

                return result?.Data ?? [];
            }
            catch (JsonException ex)
            {
                throw new AiParserException(
                    (int)HttpStatusCode.BadGateway,
                    $"AI semantic search response is not valid JSON. {ex.Message}");
            }
        }
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
