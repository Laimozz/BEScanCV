using System.Text.Json;

namespace BEScanCV.Application.DTOS;

public sealed class CvProcessingResult
{
    public string? AiDocumentId { get; set; }
    public string? CandidateName { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Position { get; set; }
    public int? TotalExperienceYears { get; set; }
    public string? Summary { get; set; }
    public string? RawText { get; set; }
    public JsonDocument? Educations { get; set; }
    public JsonDocument? ProfileData { get; set; }
    public IReadOnlyCollection<string> Skills { get; set; } = [];
}
