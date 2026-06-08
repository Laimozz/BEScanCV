using BEScanCV.Application.Interfaces.Repositories;
using BEScanCV.Domain.Entities;
using BEScanCV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BEScanCV.Infrastructure.Repositories;

public sealed class SkillRepository(BEScanCvDbContext dbContext) : ISkillRepository
{
    public Task<Skill?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return dbContext.Skills
            .FirstOrDefaultAsync(skill => skill.Id == id, cancellationToken);
    }

    public Task<Skill?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return dbContext.Skills
            .FirstOrDefaultAsync(skill => skill.Name == name, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Skill>> GetByNamesAsync(IReadOnlyCollection<string> names, CancellationToken cancellationToken = default)
    {
        var normalizedNames = names
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim().ToLowerInvariant())
            .Distinct()
            .ToArray();

        return await dbContext.Skills
            .AsNoTracking()
            .Where(skill => normalizedNames.Contains(skill.Name.ToLower()))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Skill skill, CancellationToken cancellationToken = default)
    {
        await dbContext.Skills.AddAsync(skill, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
