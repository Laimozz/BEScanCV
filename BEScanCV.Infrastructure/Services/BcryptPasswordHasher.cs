using BEScanCV.Application.Interfaces;

namespace BEScanCV.Infrastructure.Services;

public sealed class BcryptHasher : IHasher
{
    public string Hash(string stringToBeHashed)
    {
        return BCrypt.Net.BCrypt.HashPassword(stringToBeHashed);
    }

    public bool Verify(string stringToBeVerified, string hashedString)
    {
        return BCrypt.Net.BCrypt.Verify(stringToBeVerified, hashedString);
    }
}
