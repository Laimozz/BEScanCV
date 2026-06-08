namespace BEScanCV.Application.DTOS;

public sealed record CvSearchCriteriaDto(IReadOnlyDictionary<string, IReadOnlyCollection<string>> Fields)
{
    public static CvSearchCriteriaDto Empty { get; } =
        new(new Dictionary<string, IReadOnlyCollection<string>>());
}
