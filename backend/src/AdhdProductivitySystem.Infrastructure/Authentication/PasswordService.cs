using System.Security.Cryptography;
using System.Text;

namespace AdhdProductivitySystem.Infrastructure.Authentication;

/// <summary>
/// Service for handling password hashing and validation
/// </summary>
public class PasswordService
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 10000;

    /// <summary>
    /// Hashes a password with a generated salt
    /// </summary>
    /// <param name="password">Password to hash</param>
    /// <returns>Tuple containing hash and salt</returns>
    public (string hash, string salt) HashPassword(string password)
    {
        // Generate salt
        using var rng = RandomNumberGenerator.Create();
        var salt = new byte[SaltSize];
        rng.GetBytes(salt);

        // Hash password with salt
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(HashSize);

        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    /// <summary>
    /// Verifies a password against a hash and salt
    /// </summary>
    /// <param name="password">Password to verify</param>
    /// <param name="hash">Stored hash</param>
    /// <param name="salt">Stored salt</param>
    /// <returns>True if password matches, false otherwise</returns>
    public bool VerifyPassword(string password, string hash, string salt)
    {
        try
        {
            var saltBytes = Convert.FromBase64String(salt);
            var hashBytes = Convert.FromBase64String(hash);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Iterations, HashAlgorithmName.SHA256);
            var computedHash = pbkdf2.GetBytes(HashSize);

            return CryptographicOperations.FixedTimeEquals(hashBytes, computedHash);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Generates a secure random password
    /// </summary>
    /// <param name="length">Length of the password</param>
    /// <returns>Generated password</returns>
    public string GeneratePassword(int length = 12)
    {
        const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*";
        var password = new StringBuilder();
        using var rng = RandomNumberGenerator.Create();

        for (int i = 0; i < length; i++)
        {
            var randomBytes = new byte[4];
            rng.GetBytes(randomBytes);
            var randomIndex = BitConverter.ToUInt32(randomBytes, 0) % validChars.Length;
            password.Append(validChars[(int)randomIndex]);
        }

        return password.ToString();
    }

    /// <summary>
    /// Validates password strength
    /// </summary>
    /// <param name="password">Password to validate</param>
    /// <returns>True if password meets requirements, false otherwise</returns>
    public bool IsPasswordStrong(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            return false;

        bool hasUpper = password.Any(char.IsUpper);
        bool hasLower = password.Any(char.IsLower);
        bool hasDigit = password.Any(char.IsDigit);
        bool hasSpecial = password.Any(c => !char.IsLetterOrDigit(c));

        return hasUpper && hasLower && hasDigit && hasSpecial;
    }

    /// <summary>
    /// Gets password strength score (0-5)
    /// </summary>
    /// <param name="password">Password to evaluate</param>
    /// <returns>Strength score</returns>
    public int GetPasswordStrength(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return 0;

        int score = 0;

        // Length
        if (password.Length >= 8) score++;
        if (password.Length >= 12) score++;

        // Character types
        if (password.Any(char.IsUpper)) score++;
        if (password.Any(char.IsLower)) score++;
        if (password.Any(char.IsDigit)) score++;
        if (password.Any(c => !char.IsLetterOrDigit(c))) score++;

        return Math.Min(score, 5);
    }
}