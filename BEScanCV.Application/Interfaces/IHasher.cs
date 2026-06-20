namespace BEScanCV.Application.Interfaces;

public interface IHasher
{
    string Hash(string stringToBeHashed);
    bool Verify(string stringToBeVerified, string hashedString);
}
