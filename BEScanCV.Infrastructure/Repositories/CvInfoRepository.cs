using BEScanCV.Application.DTOS;
using BEScanCV.Application.Interfaces.Repositories;
using BEScanCV.Domain.Entities;
using BEScanCV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BEScanCV.Infrastructure.Repositories;

public sealed class CvInfoRepository(BEScanCvDbContext dbContext, ILogger<CvInfoRepository> logger) : ICvInfoRepository
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
            .Include(cvInfo => cvInfo.CvSkills)
            .Include(cvInfo => cvInfo.CvCertifications)
            .Include(cvInfo => cvInfo.WorkExperiences)
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

    public async Task<(IReadOnlyCollection<CvInfo> Items, int TotalCount)> GetFavoritesAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.CvInfos
            .AsNoTracking()
            .Where(cvInfo => cvInfo.IsMarked)
            .Include(cvInfo => cvInfo.CvFile)
            .Include(cvInfo => cvInfo.CvSkills)
            .Include(cvInfo => cvInfo.CvCertifications)
            .Include(cvInfo => cvInfo.WorkExperiences)
            .OrderBy(cvInfo => cvInfo.Id);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IReadOnlyCollection<CvInfo> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        CvGetAllFilterDto? filter,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.CvInfos
            .AsNoTracking()
            .Include(cvInfo => cvInfo.CvFile)
                .ThenInclude(cvFile => cvFile!.Uploader)
            .Include(cvInfo => cvInfo.CvSkills)
            .Include(cvInfo => cvInfo.CvCertifications)
            .Include(cvInfo => cvInfo.WorkExperiences)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim().ToLowerInvariant();
            query = query.Where(cv =>
                (cv.FullName != null && EF.Functions.ILike(cv.FullName.ToLower(), $"%{searchTerm}%")) ||
                (cv.Email != null && EF.Functions.ILike(cv.Email.ToLower(), $"%{searchTerm}%")) ||
                (cv.Position != null && EF.Functions.ILike(cv.Position.ToLower(), $"%{searchTerm}%")) ||
                (cv.Address != null && EF.Functions.ILike(cv.Address.ToLower(), $"%{searchTerm}%")) ||
                (cv.RawText != null && EF.Functions.ILike(cv.RawText.ToLower(), $"%{searchTerm}%")) ||
                (cv.Educations != null && EF.Functions.ILike(cv.Educations.RootElement.ToString().ToLower(), $"%{searchTerm}%")) ||
                (cv.ProfileData != null && EF.Functions.ILike(cv.ProfileData.RootElement.ToString().ToLower(), $"%{searchTerm}%")) ||
                cv.CvSkills.Any(skill => EF.Functions.ILike(skill.Name.ToLower(), $"%{searchTerm}%")));
        }

        if (filter != null)
        {
            if (filter.TotalExperienceYears.HasValue)
            {
                query = filter.TotalExperienceYears.Value switch
                {
                    0 => query.Where(cv => cv.TotalExperienceYears <= 1),
                    1 => query.Where(cv => cv.TotalExperienceYears >= 1 && cv.TotalExperienceYears <= 3),
                    3 => query.Where(cv => cv.TotalExperienceYears >= 3 && cv.TotalExperienceYears <= 5),
                    5 => query.Where(cv => cv.TotalExperienceYears >= 5),
                    _ => query
                };
            }

            if (!string.IsNullOrWhiteSpace(filter.Location))
            {
                var location = filter.Location.Trim().ToLowerInvariant();
                query = query.Where(cv => cv.Address != null && EF.Functions.ILike(cv.Address.ToLower(), $"%{location}%"));
            }

            if (!string.IsNullOrWhiteSpace(filter.Position))
            {
                var position = filter.Position.Trim().ToLowerInvariant();
                query = query.Where(cv => cv.Position != null && EF.Functions.ILike(cv.Position.ToLower(), $"%{position}%"));
            }

            if (!string.IsNullOrWhiteSpace(filter.WorkType))
            {
                var workType = filter.WorkType.Trim().ToLowerInvariant();
                query = query.Where(cv => cv.WorkType != null && EF.Functions.ILike(cv.WorkType.ToLower(), $"%{workType}%"));
            }

            if (!string.IsNullOrWhiteSpace(filter.Skills))
            {
                var requiredSkills = filter.Skills
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(s => s.ToLowerInvariant())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToArray();

                if (requiredSkills.Length > 0)
                {
                    foreach (var skill in requiredSkills)
                    {
                        var skillPattern = skill;
                        query = query.Where(cv =>
                            cv.CvSkills.Any(s => EF.Functions.ILike(s.Name.ToLower(), $"%{skillPattern}%")));
                    }
                }
            }
        }

        query = query.OrderByDescending(cv => cv.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(CvInfo cvInfo, CancellationToken cancellationToken = default)
    {
        NormalizeDateTimes(cvInfo);
        await dbContext.CvInfos.AddAsync(cvInfo, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Added CV. CvInfoId: {CvInfoId} at {Timestamp}", cvInfo.Id, DateTime.UtcNow);
    }

    public async Task UpdateAsync(CvInfo cvInfo, CancellationToken cancellationToken = default)
    {
        NormalizeDateTimes(cvInfo);
        dbContext.Entry(cvInfo).State = EntityState.Modified;
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Updated CV. CvInfoId: {CvInfoId} at {Timestamp}", cvInfo.Id, DateTime.UtcNow);
    }

    public async Task UpdateEditableDataAsync(
        CvInfo cvInfo,
        IReadOnlyCollection<string>? certifications,
        CancellationToken cancellationToken = default)
    {
        NormalizeDateTimes(cvInfo);

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

        dbContext.Entry(cvInfo).State = EntityState.Modified;

        foreach (var experience in cvInfo.WorkExperiences)
        {
            dbContext.Entry(experience).State =
                experience.Id == 0 ? EntityState.Added : EntityState.Modified;
        }

        if (certifications is not null)
        {
            var existingCertifications = await dbContext.CvCertifications
                .Where(certification => certification.CvInfoId == cvInfo.Id)
                .ToListAsync(cancellationToken);

            dbContext.CvCertifications.RemoveRange(existingCertifications);

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
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Updated editable data for CV. CvInfoId: {CvInfoId} at {Timestamp}", cvInfo.Id, DateTime.UtcNow);
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
        logger.LogInformation("Upserted extracted data for CV. CvInfoId: {CvInfoId}, Skills: {SkillCount}, Certifications: {CertCount}, Experiences: {ExpCount} at {Timestamp}", cvInfo.Id, cvSkills.Count(), cvCertifications.Count(), workExperiences.Count, DateTime.UtcNow);
    }

    private static void NormalizeDateTimes(CvInfo cvInfo)
    {
        cvInfo.CreatedAt = DateTimeUtcNormalizer.Normalize(cvInfo.CreatedAt);
        cvInfo.UpdatedAt = DateTimeUtcNormalizer.Normalize(cvInfo.UpdatedAt);
    }
}
