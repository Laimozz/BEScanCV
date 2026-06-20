using BEScanCV.Application.Interfaces.Repositories;
using BEScanCV.Domain.Entities;
using BEScanCV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BEScanCV.Infrastructure.Repositories;

public sealed class CvFileRepository(BEScanCvDbContext dbContext) : ICvFileRepository
{
    public Task<CvFile?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return dbContext.CvFiles
            .Include(cvFile => cvFile.CvInfo)
            .FirstOrDefaultAsync(cvFile => cvFile.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<CvFile>> GetByUploaderAsync(long uploadedBy, CancellationToken cancellationToken = default)
    {
        return await dbContext.CvFiles
            .AsNoTracking()
            .Include(cvFile => cvFile.CvInfo)
            .Where(cvFile => cvFile.UploadedBy == uploadedBy)
            .OrderByDescending(cvFile => cvFile.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(CvFile cvFile, CancellationToken cancellationToken = default)
    {
        NormalizeDateTimes(cvFile);
        await dbContext.CvFiles.AddAsync(cvFile, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(CvFile cvFile, CancellationToken cancellationToken = default)
    {
        NormalizeDateTimes(cvFile);
        dbContext.CvFiles.Update(cvFile);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void NormalizeDateTimes(CvFile cvFile)
    {
        cvFile.CreatedAt = DateTimeUtcNormalizer.Normalize(cvFile.CreatedAt);
        cvFile.UpdatedAt = DateTimeUtcNormalizer.Normalize(cvFile.UpdatedAt);
    }
}
