using System.Net.Http.Headers;
using System.Text.Json;
using BEScanCV.Application.DTOS;
using BEScanCV.Application.Interfaces;
using BEScanCV.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BEScanCV.Infrastructure.Services;

public sealed class AiCvProcessingClient(
    HttpClient httpClient,
    IOptions<AiServiceOptions> options,
    ILogger<AiCvProcessingClient> logger) : ICvProcessingClient
{
    private readonly AiServiceOptions _options = options.Value;

    public async Task<CvProcessingResult> SubmitAsync(
        long cvFileId,
        string requestId,
        string batchId,
        string originalFileName,
        string fileType,
        string fileUrl,
        byte[] content,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            throw new InvalidOperationException("AI service is not configured.");
        }

        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(content);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(fileType));

        form.Add(fileContent, "file", originalFileName);
        form.Add(new StringContent(cvFileId.ToString()), "cvFileId");
        form.Add(new StringContent(requestId), "requestId");
        form.Add(new StringContent(batchId), "batchId");
        form.Add(new StringContent(originalFileName), "originalFileName");
        form.Add(new StringContent(fileType), "fileType");
        form.Add(new StringContent(fileUrl), "fileUrl");

        using var request = new HttpRequestMessage(HttpMethod.Post, _options.ProcessCvPath)
        {
            Content = form
        };

        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            request.Headers.Add("X-API-Key", _options.ApiKey);
        }

        var requestUri = httpClient.BaseAddress is null
            ? _options.ProcessCvPath
            : new Uri(httpClient.BaseAddress, _options.ProcessCvPath).ToString();
        logger.LogInformation(
            "Sending CV to AI. CvFileId: {CvFileId}, FileName: {FileName}, Url: {Url}, FileSize: {FileSize}",
            cvFileId,
            originalFileName,
            requestUri,
            content.Length);

        var startedAt = DateTime.UtcNow;
        using var response = await httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var elapsedMs = (DateTime.UtcNow - startedAt).TotalMilliseconds;

        logger.LogInformation(
            "AI responded. CvFileId: {CvFileId}, StatusCode: {StatusCode}, ElapsedMs: {ElapsedMs}",
            cvFileId,
            (int)response.StatusCode,
            elapsedMs);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "AI CV upload failed. CvFileId: {CvFileId}, StatusCode: {StatusCode}, Body: {Body}",
                cvFileId,
                (int)response.StatusCode,
                responseBody);

            throw new InvalidOperationException(
                $"AI CV upload failed. StatusCode: {(int)response.StatusCode}.");
        }

        return ReadProcessingResult(responseBody);
    }

    private static CvProcessingResult ReadProcessingResult(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return new CvProcessingResult();
        }

        try
        {
            var profileData = JsonDocument.Parse(responseBody);
            var root = profileData.RootElement;
            var data = root.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == JsonValueKind.Object
                ? dataElement
                : default;
            var basicInformation = data.ValueKind == JsonValueKind.Object &&
                                   data.TryGetProperty("basic_information", out var basicInfoElement) &&
                                   basicInfoElement.ValueKind == JsonValueKind.Object
                ? basicInfoElement
                : default;

            return new CvProcessingResult
            {
                AiDocumentId = ReadDocumentId(root),
                CandidateName = ReadCandidateName(root),
                FullName = GetString(basicInformation, "name") ?? ReadCandidateName(root),
                Email = GetString(basicInformation, "email"),
                Phone = GetString(basicInformation, "phone"),
                Address = GetString(basicInformation, "address"),
                DateOfBirth = GetDateOnly(basicInformation, "date_of_birth") ??
                              GetDateOnly(basicInformation, "dateOfBirth") ??
                              GetDateOnly(data, "date_of_birth") ??
                              GetDateOnly(data, "dateOfBirth"),
                Position = GetString(data, "job_position"),
                TotalExperienceYears = GetInt(data, "total_experience_years"),
                Summary = GetString(data, "self_evaluation"),
                RawText = GetString(root, "raw_text"),
                Educations = CloneJsonDocument(data, "education_background"),
                ProfileData = profileData,
                Skills = ReadSkills(data)
            };
        }
        catch (JsonException)
        {
            return new CvProcessingResult();
        }
    }

    private static string? ReadDocumentId(JsonElement root) =>
        GetString(root, "cv_id") ??
        GetString(root, "aiDocumentId") ??
        GetString(root, "ai_document_id") ??
        GetString(root, "documentId") ??
        GetString(root, "document_id") ??
        GetString(root, "id") ??
        ReadNestedString(root, "aiDocumentId") ??
        ReadNestedString(root, "ai_document_id") ??
        ReadNestedString(root, "documentId") ??
        ReadNestedString(root, "document_id") ??
        ReadNestedString(root, "id");

    private static string? ReadCandidateName(JsonElement root) =>
        GetString(root, "candidateName") ??
        GetString(root, "candidate_name") ??
        GetString(root, "fullName") ??
        GetString(root, "full_name") ??
        ReadNestedString(root, "candidateName") ??
        ReadNestedString(root, "candidate_name") ??
        ReadNestedString(root, "fullName") ??
        ReadNestedString(root, "full_name");

    private static string? ReadNestedString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return GetString(data, propertyName);
    }

    private static string? GetString(JsonElement root, string propertyName) =>
        root.ValueKind == JsonValueKind.Object &&
        root.TryGetProperty(propertyName, out var property) &&
        property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static int? GetInt(JsonElement root, string propertyName)
    {
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var number))
        {
            return number;
        }

        return property.ValueKind == JsonValueKind.String && int.TryParse(property.GetString(), out var parsed)
            ? parsed
            : null;
    }

    private static DateOnly? GetDateOnly(JsonElement root, string propertyName)
    {
        var value = GetString(root, propertyName);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateOnly.TryParse(value, out var date) ? date : null;
    }

    private static JsonDocument? CloneJsonDocument(JsonElement root, string propertyName)
    {
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty(propertyName, out var property) ||
            property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return JsonDocument.Parse(property.GetRawText());
    }

    private static IReadOnlyCollection<string> ReadSkills(JsonElement data)
    {
        if (data.ValueKind != JsonValueKind.Object ||
            !data.TryGetProperty("skills_and_specialties", out var skillsAndSpecialties) ||
            skillsAndSpecialties.ValueKind != JsonValueKind.Object ||
            !skillsAndSpecialties.TryGetProperty("skills", out var skills) ||
            skills.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return skills.EnumerateArray()
            .Where(skill => skill.ValueKind == JsonValueKind.String)
            .Select(skill => skill.GetString())
            .Where(skill => !string.IsNullOrWhiteSpace(skill))
            .Select(skill => skill!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string GetContentType(string fileType) =>
        fileType.ToLowerInvariant() switch
        {
            "pdf" => "application/pdf",
            "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "doc" => "application/msword",
            _ => "application/octet-stream"
        };
}
