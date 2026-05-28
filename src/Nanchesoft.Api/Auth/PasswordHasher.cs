using BCryptNet = BCrypt.Net.BCrypt;

namespace Nanchesoft.Api.Auth;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public sealed class PasswordHasher : IPasswordHasher
{
    // Work factor 11 (balance entre seguridad y velocidad)
    private const int WorkFactor = 11;

    public string Hash(string password)
        => BCryptNet.HashPassword(password, WorkFactor);

    public bool Verify(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
            return false;
        try
        {
            return BCryptNet.Verify(password, hash);
        }
        catch
        {
            return false;
        }
    }
}
