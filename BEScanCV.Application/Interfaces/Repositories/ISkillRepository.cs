using BEScanCV.Domain.Entities;

namespace BEScanCV.Application.Interfaces.Repositories;

public interface ISkillRepository
{
    Task<Skill?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Skill?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Skill>> GetByNamesAsync(IReadOnlyCollection<string> names, CancellationToken cancellationToken = default);
    Task AddAsync(Skill skill, CancellationToken cancellationToken = default);
}
