namespace BEScanCV.Application.Interfaces;

public interface IHasher
{
    string Hash(string password);
    bool Verify(string password, string Hash);
}