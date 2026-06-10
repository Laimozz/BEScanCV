namespace BEScanCV.Application.DTOS;

public class CvGetAllRequest
{
    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 10;
    public string? Search { get; set; }
    public string? Extensions { get; set; } // For future implementations e.g sort, filter,....
}