using BEScanCV.Domain.Entities;

namespace BEScanCV.Application.Interfaces.Repositories;

public interface ICvFileRepository
{
    Task<CvFile?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CvFile>> GetByUploaderAsync(long uploadedBy, CancellationToken cancellationToken = default);
    Task AddAsync(CvFile cvFile, CancellationToken cancellationToken = default);
    Task UpdateAsync(CvFile cvFile, CancellationToken cancellationToken = default);
}
