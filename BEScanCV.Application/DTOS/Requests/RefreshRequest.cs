namespace BEScanCV.Application.DTOS.Requests;

public sealed record RefreshRequest
{
    public string? RefreshToken { get; init; }
}