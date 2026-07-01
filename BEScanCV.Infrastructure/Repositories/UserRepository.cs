using BEScanCV.Application.Interfaces.Repositories;
using BEScanCV.Domain.Entities;
using BEScanCV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BEScanCV.Infrastructure.Repositories;

public sealed class UserRepository(BEScanCvDbContext dbContext, ILogger<UserRepository> logger) : IUserRepository
{
    public Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return dbContext.Users
            .FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct)
{
    var result = await dbContext.Users
        .FirstOrDefaultAsync(user => user.Email == email, ct);
    if(result != null)
        logger.LogInformation("User found from the email {Email}: User ID: {Id}, User full name {FullName}", email, result.Id, result.FullName);
    else
        logger.LogInformation("No user can be found from the email {Email}", email);      
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
        NormalizeDateTimes(user);
        await dbContext.Users.AddAsync(user, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        NormalizeDateTimes(user);

        if (dbContext.Entry(user).State == EntityState.Detached)
        {
            dbContext.Users.Update(user);
        }

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

    private static void NormalizeDateTimes(User user)
    {
        user.CreatedAt = DateTimeUtcNormalizer.Normalize(user.CreatedAt);
        user.UpdatedAt = DateTimeUtcNormalizer.Normalize(user.UpdatedAt);
        user.LastActive = NormalizeLastActive(user.LastActive, user.UpdatedAt);
    }

    private static DateTime NormalizeLastActive(DateTime lastActive, DateTime fallback)
    {
        if (lastActive == DateTime.MinValue || lastActive == DateTime.MaxValue)
        {
            return fallback == DateTime.MinValue || fallback == DateTime.MaxValue
                ? DateTime.UtcNow
                : DateTimeUtcNormalizer.Normalize(fallback);
        }

        return DateTimeUtcNormalizer.Normalize(lastActive);
    }
}
