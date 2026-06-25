namespace BEScanCV.Application.DTOS;

public sealed record RefreshRequest
{
    public string? RefreshToken { get; init; }
}