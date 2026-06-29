using BEScanCV.Application.DTOS;
using BEScanCV.Domain.Entities;

namespace BEScanCV.Application.Interfaces.Repositories;

public interface ICvInfoRepository
{
    Task<CvInfo?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<CvInfo?> GetByCvFileIdAsync(long cvFileId, CancellationToken cancellationToken = default);
    Task<CvInfo?> GetByAiDocumentIdAsync(
        string aiDocumentId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CvInfo>> GetByAiDocumentIdsAsync(
        IReadOnlyCollection<string> aiDocumentIds,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CvInfo>> GetWithSkillsAsync(CancellationToken cancellationToken = default);
    Task<(IReadOnlyCollection<CvInfo> Items, int TotalCount)> GetFavoritesAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<(IReadOnlyCollection<CvInfo> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        CvGetAllFilterDto? filter,
        CancellationToken cancellationToken = default);
    Task AddAsync(CvInfo cvInfo, CancellationToken cancellationToken = default);
    Task UpdateAsync(CvInfo cvInfo, CancellationToken cancellationToken = default);
    Task UpdateEditableDataAsync(
        CvInfo cvInfo,
        IReadOnlyCollection<string>? certifications,
        CancellationToken cancellationToken = default);
    Task UpsertExtractedDataAsync(
        CvInfo cvInfo,
        IReadOnlyCollection<string> skills,
        IReadOnlyCollection<string> certifications,
        IReadOnlyCollection<CvWorkExperience> workExperiences,
        CancellationToken cancellationToken = default);
}
