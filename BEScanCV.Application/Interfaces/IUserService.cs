using BEScanCV.Application.DTOS;
using BEScanCV.Application.DTOS.Requests;
using BEScanCV.Application.DTOS.Response;
using BEScanCV.Domain.Entities;

namespace BEScanCV.Application.Interfaces;

public interface IUserService
{
    Task<GetUsersResponse> GetUsersAsync(
        int page,
        int limit,
        string? role,
        string? status,
        CancellationToken cancellationToken = default);

    Task<GetUserResponse> GetUserByIdAsync(
        long id,
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
    bool VerifyPassword(string password, string Hash);
    Task<UserDto?> GetCurrentUserAsync(long userId, CancellationToken cancellationToken);
    Task<UserDto> UpdateProfileAsync(long userId, string fullName, CancellationToken cancellationToken);
}
