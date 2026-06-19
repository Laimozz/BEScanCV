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
                .ThenInclude(cvFile => cvFile!.Uploader)
            .Include(cvInfo => cvInfo.CvSkills)
            .Include(cvInfo => cvInfo.CvCertifications)
            .Include(cvInfo => cvInfo.WorkExperiences)
            .FirstOrDefaultAsync(cvInfo => cvInfo.Id == id, cancellationToken);
    }

    public Task<CvInfo?> GetByCvFileIdAsync(long cvFileId, CancellationToken cancellationToken = default)
    {
        return dbContext.CvInfos
            .Include(cvInfo => cvInfo.CvFile)
                .ThenInclude(cvFile => cvFile!.Uploader)
            .Include(cvInfo => cvInfo.CvSkills)
            .Include(cvInfo => cvInfo.CvCertifications)
            .Include(cvInfo => cvInfo.WorkExperiences)
            .FirstOrDefaultAsync(cvInfo => cvInfo.CvFileId == cvFileId, cancellationToken);
    }

    public Task<CvInfo?> GetByAiDocumentIdAsync(
        string aiDocumentId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.CvInfos
            .Include(cvInfo => cvInfo.CvFile)
            .FirstOrDefaultAsync(
                cvInfo => cvInfo.CvFile != null &&
                          cvInfo.CvFile.AiDocumentId == aiDocumentId,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<CvInfo>> GetByAiDocumentIdsAsync(
        IReadOnlyCollection<string> aiDocumentIds,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.CvInfos
            .AsNoTracking()
            .Include(cvInfo => cvInfo.CvFile)
            .Where(cvInfo =>
                cvInfo.CvFile != null &&
                cvInfo.CvFile.AiDocumentId != null &&
                aiDocumentIds.Contains(cvInfo.CvFile.AiDocumentId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<CvInfo>> GetWithSkillsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.CvInfos
            .AsNoTracking()
            .Include(cvInfo => cvInfo.CvFile)
                .ThenInclude(cvFile => cvFile!.Uploader)
            .Include(cvInfo => cvInfo.CvSkills)
            .Include(cvInfo => cvInfo.CvCertifications)
            .Include(cvInfo => cvInfo.WorkExperiences)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(CvInfo cvInfo, CancellationToken cancellationToken = default)
    {
        NormalizeDateTimes(cvInfo);
        await dbContext.CvInfos.AddAsync(cvInfo, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(CvInfo cvInfo, CancellationToken cancellationToken = default)
    {
        NormalizeDateTimes(cvInfo);
        dbContext.Entry(cvInfo).State = EntityState.Modified;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateEditableDataAsync(
        CvInfo cvInfo,
        IReadOnlyCollection<string> certifications,
        CancellationToken cancellationToken = default)
    {
        NormalizeDateTimes(cvInfo);

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var existingCertifications = await dbContext.CvCertifications
            .Where(certification => certification.CvInfoId == cvInfo.Id)
            .ToListAsync(cancellationToken);

        dbContext.CvCertifications.RemoveRange(existingCertifications);
        dbContext.Entry(cvInfo).State = EntityState.Modified;

        foreach (var experience in cvInfo.WorkExperiences)
        {
            dbContext.Entry(experience).State =
                experience.Id == 0 ? EntityState.Added : EntityState.Modified;
        }

        if (certifications.Count > 0)
        {
            await dbContext.CvCertifications.AddRangeAsync(
                certifications.Select(certification => new CvCertification
                {
                    CvInfoId = cvInfo.Id,
                    Name = certification
                }),
                cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task UpsertExtractedDataAsync(
        CvInfo cvInfo,
        IReadOnlyCollection<string> skills,
        IReadOnlyCollection<string> certifications,
        IReadOnlyCollection<CvWorkExperience> workExperiences,
        CancellationToken cancellationToken = default)
    {
        NormalizeDateTimes(cvInfo);

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

        if (cvInfo.Id == 0)
        {
            await dbContext.CvInfos.AddAsync(cvInfo, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            dbContext.CvSkills.RemoveRange(cvInfo.CvSkills);
            dbContext.CvCertifications.RemoveRange(cvInfo.CvCertifications);
            dbContext.CvWorkExperiences.RemoveRange(cvInfo.WorkExperiences);
            dbContext.Entry(cvInfo).State = EntityState.Modified;
        }

        var cvSkills = skills
            .Where(skill => !string.IsNullOrWhiteSpace(skill))
            .Select(skill => skill.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(skill => new CvSkill
            {
                CvInfoId = cvInfo.Id,
                Name = skill
            });

        var cvCertifications = certifications
            .Where(certification => !string.IsNullOrWhiteSpace(certification))
            .Select(certification => certification.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(certification => new CvCertification
            {
                CvInfoId = cvInfo.Id,
                Name = certification
            });

        foreach (var experience in workExperiences)
        {
            experience.CvInfoId = cvInfo.Id;
        }

        await dbContext.CvSkills.AddRangeAsync(cvSkills, cancellationToken);
        await dbContext.CvCertifications.AddRangeAsync(cvCertifications, cancellationToken);
        await dbContext.CvWorkExperiences.AddRangeAsync(workExperiences, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static void NormalizeDateTimes(CvInfo cvInfo)
    {
        cvInfo.CreatedAt = DateTimeUtcNormalizer.Normalize(cvInfo.CreatedAt);
        cvInfo.UpdatedAt = DateTimeUtcNormalizer.Normalize(cvInfo.UpdatedAt);
    }
}
