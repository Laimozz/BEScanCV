using BEScanCV.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BEScanCV.Infrastructure.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAdminAsync(
        BEScanCvDbContext context,
        string email,
        string password,
        string fullName,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var existingUser = await context.Users
            .FirstOrDefaultAsync(
                user => user.Email == normalizedEmail,
                cancellationToken);

        var now = DateTime.UtcNow;

        if (existingUser is not null)
        {
            existingUser.Role = "admin";
            existingUser.Status = "active";
            existingUser.UpdatedAt = now;
            await context.SaveChangesAsync(cancellationToken);
            return;
        }

        var admin = new User
        {
            FullName = string.IsNullOrWhiteSpace(fullName)
                ? "System Admin"
                : fullName.Trim(),
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = "admin",
            Status = "active",
            CreatedAt = now,
            UpdatedAt = now,
            LastActive = now
        };

        context.Users.Add(admin);
        await context.SaveChangesAsync(cancellationToken);
    }
}
