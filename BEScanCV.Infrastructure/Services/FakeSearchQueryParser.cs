using BEScanCV.Application.DTOS;
using BEScanCV.Application.Interfaces;

namespace BEScanCV.Infrastructure.Services;

public sealed class FakeSearchQueryParser : ISearchQueryParser
{
    public Task<CvSearchCriteriaDto> ParseAsync(string query, CancellationToken cancellationToken = default)
    {
        IReadOnlyDictionary<string, IReadOnlyCollection<string>> fields =
            new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["Skill"] = ["Java"],
                ["Exp"] = ["3"]
            };

        return Task.FromResult(new CvSearchCriteriaDto(fields));
    }
}
