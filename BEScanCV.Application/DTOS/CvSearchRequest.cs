namespace BEScanCV.Application.DTOS;

public sealed record CvSearchRequest(string Query, int Page = 1);
