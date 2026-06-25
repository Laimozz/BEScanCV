using BEScanCV.Application.DTOS;

namespace BEScanCV.Application.Interfaces;

public interface ITalentPoolService
{
    /// <summary>
    /// Lấy danh sách CV có is_marked = true, có phân trang.
    /// </summary>
    Task<TalentPoolResponse> GetTalentPoolAsync(
        TalentPoolRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Đánh dấu hoặc bỏ đánh dấu CV vào Talent Pool.
    /// Trả về (isMarked, object) — controller tự chọn response shape tùy theo isMarked.
    /// </summary>
    Task<(bool isMarked, object data)> MarkTalentAsync(
        long cvInfoId,
        bool isMarked,
        CancellationToken cancellationToken = default);
}
