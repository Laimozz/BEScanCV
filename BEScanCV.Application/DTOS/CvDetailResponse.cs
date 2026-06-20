using System.Text.Json;
using System.Text.Json.Serialization;

namespace BEScanCV.Application.DTOS;

public sealed class CvDetailResponse
{
    [JsonPropertyName("cv_info_id")]
    public long CvInfoId { get; set; }

    [JsonPropertyName("cv_file_id")]
    public long CvFileId { get; set; }

    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("position")]
    public string? Position { get; set; }

    [JsonPropertyName("total_experience_years")]
    public int? TotalExperienceYears { get; set; }

    [JsonPropertyName("date_of_birth")]
    public DateOnly? DateOfBirth { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("educations")]
    public JsonElement? Educations { get; set; }

    [JsonPropertyName("profile_data")]
    public JsonElement? ProfileData { get; set; }

    [JsonPropertyName("raw_text")]
    public string? RawText { get; set; }

    [JsonPropertyName("skills")]
    public string[] Skills { get; set; } = [];

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("original_file_name")]
    public string OriginalFileName { get; set; } = string.Empty;

    [JsonPropertyName("file_type")]
    public string FileType { get; set; } = string.Empty;

    [JsonPropertyName("file_size")]
    public long FileSize { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("uploaded_by")]
    public CvUploaderDto UploadedBy { get; set; } = new(0, string.Empty);

    /// <summary>
    /// URL công khai để FE truy cập file PDF trực tiếp.
    /// - File local D:\PDFLocal → http://BE_IP:port/files/ten-file.pdf
    /// - File remote (cloud)   → URL gốc của cloud storage
    /// </summary>
    [JsonPropertyName("pdf_url")]
    public string? PdfUrl { get; set; }
}
