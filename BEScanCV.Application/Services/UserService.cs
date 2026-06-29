using System.Security.Cryptography;
using BEScanCV.Application.DTOS;
using BEScanCV.Application.DTOS.Requests;
using BEScanCV.Application.DTOS.Response;
using BEScanCV.Application.Interfaces;
using BEScanCV.Application.Interfaces.Repositories;
using BEScanCV.Domain.Entities;

namespace BEScanCV.Application.Services;

public sealed class UserService(IUserRepository userRepository, IHasher Hasher, IEmailService emailService, IJwtService jwtService) : IUserService
{
    private const string TemporaryPasswordCharacters = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@$?-";

    private static readonly HashSet<string> ValidRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin", "recruiter", "interviewer"
    };

    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "active", "inactive"
    };

    public async Task<GetUsersResponse> GetUsersAsync(
        int page,
        int limit,
        string? role,
        string? status,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (limit < 1) limit = 10;
        if (limit > 100) limit = 100;

        var (items, totalCount) = await userRepository.GetAllAsync(
            page, limit, role, status, cancellationToken);

        var totalPages = (int)Math.Ceiling((double)totalCount / limit);

        var userItems = items.Select(u => new UserItemDto
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email,
            Role = u.Role,
            Status = u.Status,
            LastActive = u.LastActive
        }).ToList();

        return new GetUsersResponse
        {
            Items = userItems,
            Meta = new PaginationMetaDto(totalCount, page, limit, totalPages)
        };
    }

    public async Task<GetUserResponse> GetUserByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("User not found");

        return new GetUserResponse
        {
            User = new UserItemDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                Status = user.Status,
                LastActive = user.UpdatedAt
            }
        };
    }

    public async Task<CreateUserResponse> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new ArgumentException("Full name is required.");

        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ArgumentException("Email is required.");

        var password = GeneratePassword(8);


        if (!string.IsNullOrWhiteSpace(request.Role) && !ValidRoles.Contains(request.Role))
            throw new ArgumentException($"Invalid role. Allowed values: {string.Join(", ", ValidRoles)}");

        var emailExists = await userRepository.ExistsByEmailAsync(request.Email, cancellationToken);
        if (emailExists)
            throw new InvalidOperationException("Email already exists");


        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = Hasher.Hash(password),
            Role = string.IsNullOrWhiteSpace(request.Role) ? "recruiter" : request.Role.ToLowerInvariant(),
            Status = "active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await userRepository.AddAsync(user, cancellationToken);
        await emailService.SendAccountCreatedEmailAsync(request.Email, password);

        return new CreateUserResponse { Id = user.Id };
    }

    public async Task UpdateUserAsync(
        long id,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("User not found");

        if (request.FullName is not null)
        {
            if (string.IsNullOrWhiteSpace(request.FullName))
                throw new ArgumentException("Full name cannot be empty.");
            user.FullName = request.FullName.Trim();
        }

        if (request.Role is not null)
        {
            if (!ValidRoles.Contains(request.Role))
                throw new ArgumentException($"Invalid role. Allowed values: {string.Join(", ", ValidRoles)}");
            user.Role = request.Role;
        }

        if (request.Status is not null)
        {
            if (!ValidStatuses.Contains(request.Status))
                throw new ArgumentException($"Invalid status. Allowed values: {string.Join(", ", ValidStatuses)}");
            user.Status = request.Status;
        }

        user.UpdatedAt = DateTime.UtcNow;
        await userRepository.UpdateAsync(user, cancellationToken);
    }

    public async Task DeleteUserAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("User not found");

        await userRepository.DeleteAsync(user, cancellationToken);
    }

    public async Task ChangePasswordAsync(
        long userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
            throw new ArgumentException("Current password is required.");

        if (string.IsNullOrWhiteSpace(request.NewPassword))
            throw new ArgumentException("New password is required.");

        if (string.IsNullOrWhiteSpace(request.ConfirmPassword))
            throw new ArgumentException("Confirm new password is required.");

        if (request.NewPassword != request.ConfirmPassword)
            throw new ArgumentException("New password and confirm password do not match.");

        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found");

        if (!Hasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new InvalidOperationException("Current password is incorrect.");

        user.PasswordHash = Hasher.Hash(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await userRepository.UpdateAsync(user, cancellationToken);
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => userRepository.GetByEmailAsync(email, cancellationToken);

    public bool VerifyPassword(string password, string Hash)
        => Hasher.Verify(password, Hash);

    private static string GeneratePassword(int length)
    {
        return new string(Enumerable.Range(0, length)
        .Select(_ => TemporaryPasswordCharacters[RandomNumberGenerator.GetInt32(TemporaryPasswordCharacters.Length)])
        .ToArray());
    }

    public async Task<UserDto?> GetCurrentUserAsync(long userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found");

        return new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            Status = user.Status,
            LastActive = user.LastActive,
        };
    }

    public async Task<UserDto> UpdateProfileAsync(long userId, string fullName, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found");

        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name cannot be empty.");

        user.FullName = fullName.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        await userRepository.UpdateAsync(user, cancellationToken);

        return new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            Status = user.Status,
            LastActive = user.UpdatedAt,
        };
    }
}