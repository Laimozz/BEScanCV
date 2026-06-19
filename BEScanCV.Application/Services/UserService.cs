using BEScanCV.Application.DTOS;
using BEScanCV.Application.Interfaces;
using BEScanCV.Application.Interfaces.Repositories;
using BEScanCV.Domain.Entities;
using System.Security.Cryptography;

namespace BEScanCV.Application.Services;

public sealed class UserService(IUserRepository userRepository, IPasswordHasher passwordHasher, IEmailService emailService) : IUserService
{
    private const string TemporaryPasswordCharacters = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@$?-";

    private static readonly HashSet<string> ValidRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Admin", "Recruiter", "Interviewer"
    };

    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Active", "Inactive"
    };

    public async Task<GetUsersResponse> GetUsersAsync(
        int page,
        int pageSize,
        string? role,
        string? status,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var (items, totalCount) = await userRepository.GetAllAsync(
            page, pageSize, role, status, cancellationToken);

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        var userItems = items.Select(u => new UserItemDto
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email,
            Role = u.Role,
            Status = u.Status,
            LastActive = u.UpdatedAt
        }).ToList();

        return new GetUsersResponse
        {
            Items = userItems,
            Pagination = new UserPaginationDto
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalCount,
                TotalPages = totalPages
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

        var temporaryPassword = GenerateTemporaryPassword();

        if (!string.IsNullOrWhiteSpace(request.Role) && !ValidRoles.Contains(request.Role))
            throw new ArgumentException($"Invalid role. Allowed values: {string.Join(", ", ValidRoles)}");

        var emailExists = await userRepository.ExistsByEmailAsync(request.Email, cancellationToken);
        if (emailExists)
            throw new InvalidOperationException("Email already exists");


        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHasher.Hash(temporaryPassword),
            Role = string.IsNullOrWhiteSpace(request.Role) ? "Recruiter" : request.Role,
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await userRepository.AddAsync(user, cancellationToken);
        await emailService.SendAccountCreatedEmailAsync(request.Email, temporaryPassword);

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

        if (string.IsNullOrWhiteSpace(request.ConfirmNewPassword))
            throw new ArgumentException("Confirm new password is required.");

        if (request.NewPassword != request.ConfirmNewPassword)
            throw new ArgumentException("New password and confirm password do not match.");

        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found");

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new InvalidOperationException("Current password is incorrect.");

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await userRepository.UpdateAsync(user, cancellationToken);
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => userRepository.GetByEmailAsync(email, cancellationToken);

    public bool VerifyPassword(string password, string passwordHash)
        => passwordHasher.Verify(password, passwordHash);

    private static string GenerateTemporaryPassword()
        => RandomNumberGenerator.GetString(TemporaryPasswordCharacters, 16);
}
