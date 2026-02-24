using System.Security.Cryptography;

namespace AgroSolution.Identity.Infra.Services;

/// <summary>
/// PBKDF2-SHA256 password hasher using only BCL System.Security.Cryptography APIs.
/// Format stored: Base64(salt + hash) where salt=16 bytes, hash=32 bytes, iterations=310000.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize       = 16;
    private const int HashSize       = 32;
    private const int Iterations     = 310_000;
    private const char Separator     = ':';

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);

        return $"{Convert.ToBase64String(salt)}{Separator}{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string storedHash)
    {
        var parts = storedHash.Split(Separator);
        if (parts.Length != 2) return false;

        byte[] salt, expectedHash;
        try
        {
            salt         = Convert.FromBase64String(parts[0]);
            expectedHash = Convert.FromBase64String(parts[1]);
        }
        catch { return false; }

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
