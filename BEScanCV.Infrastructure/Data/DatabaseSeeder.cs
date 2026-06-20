using BEScanCV.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BEScanCV.Infrastructure.Data;

public static class DatabaseSeeder
{
    /// <summary>
    /// Seed 5 users vào InMemory DB khi khởi động.
    /// Password mặc định: Password@123
    /// </summary>
    public static async Task SeedUsersAsync(BEScanCvDbContext context)
    {
        if (await context.Users.AnyAsync())
            return; // Đã có dữ liệu, bỏ qua

        var now = DateTime.UtcNow;

        var users = new List<User>
        {
            new()
            {
                FullName = "Nguyen Van An",
                Email = "an.nguyen@recruitai.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password@123"),
                Role = "Admin",
                Status = "Active",
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                FullName = "Tran Thi Bich",
                Email = "bich.tran@recruitai.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password@123"),
                Role = "Recruiter",
                Status = "Active",
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                FullName = "Le Van Cuong",
                Email = "cuong.le@recruitai.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password@123"),
                Role = "Recruiter",
                Status = "Active",
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                FullName = "Pham Thi Dung",
                Email = "dung.pham@recruitai.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password@123"),
                Role = "Interviewer",
                Status = "Active",
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                FullName = "Hoang Van Em",
                Email = "em.hoang@recruitai.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password@123"),
                Role = "Interviewer",
                Status = "Active",
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        context.Users.AddRange(users);
        await context.SaveChangesAsync();
    }
}
