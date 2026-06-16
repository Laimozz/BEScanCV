using System.Text.Json.Serialization;

namespace BEScanCV.Application.DTOS;

public class CvSearchResultDto
{
    [JsonPropertyName("full_name")]
    public string FullName { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; }

    [JsonPropertyName("cv_file_id")]
    public long CvFileId { get; set; }

    [JsonPropertyName("skills")]
    public string[] Skills { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("uploaded_by")]
    public CvUploaderDto UploadedBy { get; set; }

    /// <summary>
    /// URL công khai để FE truy cập file PDF trực tiếp.
    /// - File local D:\PDFLocal → http://BE_IP:port/files/ten-file.pdf
    /// - File remote (cloud)   → URL gốc của cloud storage
    /// </summary>
    [JsonPropertyName("pdf_url")]
    public string? PdfUrl { get; set; }

    public CvSearchResultDto(
        string fullName,
        string email,
        long cvFileId,
        string[] skills,
        DateTime createdAt,
        CvUploaderDto uploadedBy,
        string? pdfUrl = null)
    {
        FullName = fullName;
        Email = email;
        CvFileId = cvFileId;
        Skills = skills;
        CreatedAt = createdAt;
        UploadedBy = uploadedBy;
        PdfUrl = pdfUrl;
    }
}
