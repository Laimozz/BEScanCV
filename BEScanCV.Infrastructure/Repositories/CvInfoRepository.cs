using BEScanCV.Application.Interfaces.Repositories;
using BEScanCV.Domain.Entities;
using BEScanCV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BEScanCV.Infrastructure.Repositories;

public sealed class CvInfoRepository(BEScanCvDbContext dbContext) : ICvInfoRepository
{
    public Task<CvInfo?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return dbContext.CvInfos
            .Include(cvInfo => cvInfo.CvFile)
            .Include(cvInfo => cvInfo.CvSkills)
                .ThenInclude(cvSkill => cvSkill.Skill)
            .FirstOrDefaultAsync(cvInfo => cvInfo.Id == id, cancellationToken);
    }

    public Task<CvInfo?> GetByCvFileIdAsync(long cvFileId, CancellationToken cancellationToken = default)
    {
        return dbContext.CvInfos
            .Include(cvInfo => cvInfo.CvFile)
            .Include(cvInfo => cvInfo.CvSkills)
                .ThenInclude(cvSkill => cvSkill.Skill)
            .FirstOrDefaultAsync(cvInfo => cvInfo.CvFileId == cvFileId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<CvInfo>> GetWithSkillsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.CvInfos
            .AsNoTracking()
            .Include(cvInfo => cvInfo.CvFile)
            .Include(cvInfo => cvInfo.CvSkills)
                .ThenInclude(cvSkill => cvSkill.Skill)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(CvInfo cvInfo, CancellationToken cancellationToken = default)
    {
        await dbContext.CvInfos.AddAsync(cvInfo, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(CvInfo cvInfo, CancellationToken cancellationToken = default)
    {
        dbContext.CvInfos.Update(cvInfo);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
