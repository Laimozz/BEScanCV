namespace BEScanCV.Application.DTOS;

public class CvGetAllFilterDto
{
    public int? total_experience_years  { get; set; }
    public string? skills { get; set; }
    public string? position { get; set; }
    public string? location { get; set; }
    public string? work_type { get; set; }
}
