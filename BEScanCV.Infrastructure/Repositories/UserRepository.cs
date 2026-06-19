using BEScanCV.Application.Interfaces.Repositories;
using BEScanCV.Domain.Entities;
using BEScanCV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BEScanCV.Infrastructure.Repositories;

public sealed class UserRepository(BEScanCvDbContext dbContext) : IUserRepository
{
    public Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return dbContext.Users
            .FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct)
{
    Console.WriteLine($"[DEBUG] Looking up email: '{email}'"); // Add this
    var result = await dbContext.Users
        .FirstOrDefaultAsync(user => user.Email == email, ct);
    Console.WriteLine($"[DEBUG] Found user: {result?.Id ?? 0}"); // Add this
    return result;
}
    public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetAllAsync(
        int page,
        int pageSize,
        string? role = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(u => u.Role == role);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(u => u.Status == status);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await dbContext.Users.AddAsync(user, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        dbContext.Users.Update(user);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(User user, CancellationToken cancellationToken = default)
    {
        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return dbContext.Users
            .AnyAsync(user => user.Email == email, cancellationToken);
    }

    public Task<bool> ExistsByEmailAsync(string email, long excludeUserId, CancellationToken cancellationToken = default)
    {
        return dbContext.Users
            .AnyAsync(user => user.Email == email && user.Id != excludeUserId, cancellationToken);
    }
}
