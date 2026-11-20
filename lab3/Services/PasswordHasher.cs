using System.Security.Cryptography;
using System.Text;

namespace lab3.Services;

public static class PasswordHasher
{
    public static string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static bool Verify(string password, string expectedHash)
    {
        var actualHash = Hash(password);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(actualHash),
            Encoding.UTF8.GetBytes(expectedHash));
    }
}
