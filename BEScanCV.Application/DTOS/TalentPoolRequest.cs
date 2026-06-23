namespace BEScanCV.Application.DTOS;

public sealed class TalentPoolRequest
{
    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 10;
}
