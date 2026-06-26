using BEScanCV.Application.Interfaces;

namespace BEScanCV.Infrastructure.Services;

public sealed class BcryptHasher : IHasher
{
    public string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool Verify(string password, string Hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, Hash);
    }
}