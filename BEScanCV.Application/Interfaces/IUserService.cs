using BEScanCV.Application.DTOS;

namespace BEScanCV.Application.Interfaces;

public interface IUserService
{
    Task<GetUsersResponse> GetUsersAsync(
        int page,
        int pageSize,
        string? role,
        string? status,
        CancellationToken cancellationToken = default);

    Task<CreateUserResponse> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default);

    Task UpdateUserAsync(
        long id,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteUserAsync(
        long id,
        CancellationToken cancellationToken = default);

    Task ChangePasswordAsync(
        long userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default);
}
