using System.Security.Cryptography;
using BEScanCV.Application.DTOS;
using BEScanCV.Application.DTOS.Requests;
using BEScanCV.Application.DTOS.Response;
using BEScanCV.Application.Interfaces;
using BEScanCV.Application.Interfaces.Repositories;
using BEScanCV.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace BEScanCV.Application.Services;

public sealed class UserService(IUserRepository userRepository, IHasher Hasher, IEmailService emailService, IJwtService jwtService, ILogger<UserService> logger) : IUserService
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

        logger.LogInformation("Retrieved {Count} users (Page: {Page}, Limit: {Limit}) at {Timestamp}", userItems.Count, page, limit, DateTime.UtcNow);
        return new GetUsersResponse
        {
            Items = userItems,
            Meta = new PaginationMetaDto(totalCount, page, limit, totalPages)
        };
    }

    public async Task<GetUserResponse> GetUserByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(id, cancellationToken);
        if(user == null)
        {
            logger.LogWarning("User with ID {UserId} not found at {Timestamp}", id, DateTime.UtcNow);
            throw new KeyNotFoundException("User not found");
        }

        logger.LogInformation("Retrieved user with ID {UserId} at {Timestamp}", id , DateTime.UtcNow);
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
        {
            logger.LogWarning("Attempted to create user with empty full name at {Timestamp}", DateTime.UtcNow);
            throw new ArgumentException("Full name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            logger.LogWarning("Attempted to create user with empty email at {Timestamp}", DateTime.UtcNow);
            throw new ArgumentException("Email is required.");
        }

        var password = GeneratePassword(8);

        var normalizedRole = NormalizeOptionalValue(request.Role);
        if (normalizedRole is not null && !ValidRoles.Contains(normalizedRole))
        {
            logger.LogWarning("Attempted to create user with invalid role at {Timestamp}", DateTime.UtcNow);
            throw new ArgumentException($"Invalid role. Allowed values: {string.Join(", ", ValidRoles)}");
        }

        var emailExists = await userRepository.ExistsByEmailAsync(request.Email, cancellationToken);
        if (emailExists)
        {
            logger.LogWarning("Attempted to create user with existing email at {Timestamp}", DateTime.UtcNow);
            throw new InvalidOperationException("Email already exists");
        }

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = Hasher.Hash(password),
            Role = normalizedRole ?? "recruiter",
            Status = "active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await userRepository.AddAsync(user, cancellationToken);
        await emailService.SendAccountCreatedEmailAsync(request.Email, password);

        logger.LogInformation("Created user with ID {UserId} and email {Email} at {Timestamp}", user.Id, user.Email, DateTime.UtcNow);

        return new CreateUserResponse { Id = user.Id };
    }

    public async Task UpdateUserAsync(
        long id,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(id, cancellationToken);
        if (user == null)
        {
            logger.LogWarning("User with ID {UserId} not found for update at {Timestamp}", id, DateTime.UtcNow);
            throw new KeyNotFoundException("User not found");
        }

        if (request.FullName is not null)
        {
            if (string.IsNullOrWhiteSpace(request.FullName))
            {
                logger.LogWarning("Attempted to update user ID {UserId} with empty full name at {Timestamp}", id, DateTime.UtcNow);
                throw new ArgumentException("Full name cannot be empty.");
            }
            user.FullName = request.FullName.Trim();
        }

        if (request.Role is not null)
        {
            var normalizedRole = NormalizeRequiredValue(request.Role);
            if (!ValidRoles.Contains(normalizedRole))
            {
                logger.LogWarning("Attempted to update user ID {UserId} with invalid role at {Timestamp}", id, DateTime.UtcNow);
                throw new ArgumentException($"Invalid role. Allowed values: {string.Join(", ", ValidRoles)}");
            }
            user.Role = normalizedRole;
        }

        if (request.Status is not null)
        {
            var normalizedStatus = NormalizeRequiredValue(request.Status);
            if (!ValidStatuses.Contains(normalizedStatus))
            {
                logger.LogWarning("Attempted to update user ID {UserId} with invalid status at {Timestamp}", id, DateTime.UtcNow);
                throw new ArgumentException($"Invalid status. Allowed values: {string.Join(", ", ValidStatuses)}");
            }
            user.Status = normalizedStatus;
        }

        user.UpdatedAt = DateTime.UtcNow;
        await userRepository.UpdateAsync(user, cancellationToken);

        logger.LogInformation("Updated user with ID {UserId} at {Timestamp}", id, DateTime.UtcNow);
    }

    public async Task DeleteUserAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(id, cancellationToken);
        if (user == null)
        {
            logger.LogWarning("User with ID {UserId} not found for deletion at {Timestamp}", id, DateTime.UtcNow);
            throw new KeyNotFoundException("User not found");
        }

        await userRepository.DeleteAsync(user, cancellationToken);

        logger.LogInformation("Deleted user with ID {UserId} at {Timestamp}", id, DateTime.UtcNow);
    }

    public async Task ChangePasswordAsync(
        long userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            logger.LogWarning("Attempted to change password for user ID {UserId} with empty current password at {Timestamp}", userId, DateTime.UtcNow);
            throw new ArgumentException("Current password is required.");
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            logger.LogWarning("Attempted to change password for user ID {UserId} with empty new password at {Timestamp}", userId, DateTime.UtcNow);
            throw new ArgumentException("New password is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ConfirmPassword))
        {
            logger.LogWarning("Attempted to change password for user ID {UserId} with empty confirm password at {Timestamp}", userId, DateTime.UtcNow);
            throw new ArgumentException("Confirm new password is required.");
        }

        if (request.NewPassword != request.ConfirmPassword)
        {
            logger.LogWarning("Attempted to change password for user ID {UserId} with mismatched passwords at {Timestamp}", userId, DateTime.UtcNow);
            throw new ArgumentException("New password and confirm password do not match.");
        }

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            logger.LogWarning("User with ID {UserId} not found for password change at {Timestamp}", userId, DateTime.UtcNow);
            throw new KeyNotFoundException("User not found");
        }

        if (!Hasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            logger.LogWarning("Attempted to change password for user ID {UserId} with incorrect current password at {Timestamp}", userId, DateTime.UtcNow);
            throw new InvalidOperationException("Current password is incorrect.");
        }

        user.PasswordHash = Hasher.Hash(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await userRepository.UpdateAsync(user, cancellationToken);

        logger.LogInformation("Changed password for user with ID {UserId} at {Timestamp}", userId, DateTime.UtcNow);
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

    private static string? NormalizeOptionalValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().ToLowerInvariant();
    }

    private static string NormalizeRequiredValue(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    public async Task<UserDto?> GetCurrentUserAsync(long userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            logger.LogWarning("User with ID {UserId} not found at {Timestamp}", userId, DateTime.UtcNow);
            throw new KeyNotFoundException("User not found");
        }

        logger.LogInformation("Retrieved current user with ID {UserId} at {Timestamp}", userId, DateTime.UtcNow);

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
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            logger.LogWarning("User with ID {UserId} not found for profile update at {Timestamp}", userId, DateTime.UtcNow);
            throw new KeyNotFoundException("User not found");
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            logger.LogWarning("Attempted to update profile for user ID {UserId} with empty full name at {Timestamp}", userId, DateTime.UtcNow);
            throw new ArgumentException("Full name cannot be empty.");
        }

        user.FullName = fullName.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        await userRepository.UpdateAsync(user, cancellationToken);

        logger.LogInformation("Updated profile for user with ID {UserId} at {Timestamp}", userId, DateTime.UtcNow);

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
