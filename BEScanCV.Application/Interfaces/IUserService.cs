using BEScanCV.Application.DTOS;
using BEScanCV.Domain.Entities;

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

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    bool VerifyPassword(string password, string passwordHash);
}
