using BEScanCV.Domain.Entities;

namespace BEScanCV.Application.Interfaces.Repositories;

public interface ICvSkillRepository
{
    Task<CvSkill?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CvSkill>> GetByCvInfoIdAsync(long cvInfoId, CancellationToken cancellationToken = default);
    Task AddAsync(CvSkill cvSkill, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IReadOnlyCollection<CvSkill> cvSkills, CancellationToken cancellationToken = default);
    Task UpdateAsync(CvSkill cvSkill, CancellationToken cancellationToken = default);
    Task DeleteAsync(CvSkill cvSkill, CancellationToken cancellationToken = default);
    Task DeleteByCvInfoIdAsync(long cvInfoId, CancellationToken cancellationToken = default);
}
