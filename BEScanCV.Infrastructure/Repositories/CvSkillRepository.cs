using BEScanCV.Application.Interfaces.Repositories;
using BEScanCV.Domain.Entities;
using BEScanCV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BEScanCV.Infrastructure.Repositories;

public sealed class CvSkillRepository(BEScanCvDbContext dbContext) : ICvSkillRepository
{
    public Task<CvSkill?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return dbContext.CvSkills
            .FirstOrDefaultAsync(cvSkill => cvSkill.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<CvSkill>> GetByCvInfoIdAsync(long cvInfoId, CancellationToken cancellationToken = default)
    {
        return await dbContext.CvSkills
            .AsNoTracking()
            .Where(cvSkill => cvSkill.CvInfoId == cvInfoId)
            .OrderBy(cvSkill => cvSkill.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(CvSkill cvSkill, CancellationToken cancellationToken = default)
    {
        await dbContext.CvSkills.AddAsync(cvSkill, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IReadOnlyCollection<CvSkill> cvSkills, CancellationToken cancellationToken = default)
    {
        await dbContext.CvSkills.AddRangeAsync(cvSkills, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(CvSkill cvSkill, CancellationToken cancellationToken = default)
    {
        dbContext.CvSkills.Update(cvSkill);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(CvSkill cvSkill, CancellationToken cancellationToken = default)
    {
        dbContext.CvSkills.Remove(cvSkill);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteByCvInfoIdAsync(long cvInfoId, CancellationToken cancellationToken = default)
    {
        var cvSkills = await dbContext.CvSkills
            .Where(cvSkill => cvSkill.CvInfoId == cvInfoId)
            .ToListAsync(cancellationToken);

        dbContext.CvSkills.RemoveRange(cvSkills);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
