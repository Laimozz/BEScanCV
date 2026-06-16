using BEScanCV.Application.DTOS;

namespace BEScanCV.Application.Interfaces;

public interface ICvDetailService
{
    /// <summary>
    /// Lấy thông tin chi tiết CV kèm pdf_url công khai để FE truy cập trực tiếp.
    /// Trả về null nếu không tìm thấy CV.
    /// </summary>
    Task<CvDetailResponse?> GetByCvFileIdAsync(
        long cvFileId,
        string requestBaseUrl,
        CancellationToken cancellationToken = default);
}
