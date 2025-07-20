using System.Security.Cryptography;
using System.Text;

namespace AdhdProductivitySystem.Infrastructure.Authentication;

/// <summary>
/// Service for handling password hashing and validation
/// </summary>
public class PasswordService
{
    private const int SaltSize = 32; // 增加 salt 大小提高安全性
    private const int HashSize = 64; // 增加 hash 大小
    private const int Iterations = 100000; // 增加迭代次數提高安全性（符合OWASP建議）

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
    /// Validates password strength with enhanced security requirements
    /// </summary>
    /// <param name="password">Password to validate</param>
    /// <returns>True if password meets requirements, false otherwise</returns>
    public bool IsPasswordStrong(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            return false;

        // 檢查常見弱密碼
        if (IsCommonWeakPassword(password))
            return false;

        // 檢查重複字符模式
        if (HasRepeatingPatterns(password))
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

    /// <summary>
    /// 檢查是否為常見弱密碼
    /// </summary>
    /// <param name="password">要檢查的密碼</param>
    /// <returns>如果是常見弱密碼則返回true</returns>
    private static bool IsCommonWeakPassword(string password)
    {
        var commonPasswords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "password", "123456", "123456789", "qwerty", "abc123", "password123",
            "admin", "letmein", "welcome", "monkey", "1234567890", "dragon",
            "master", "hello", "freedom", "whatever", "qazwsx", "trustno1",
            "superman", "batman", "football", "baseball", "basketball", "soccer"
        };

        return commonPasswords.Contains(password.ToLowerInvariant());
    }

    /// <summary>
    /// 檢查是否有重複字符模式
    /// </summary>
    /// <param name="password">要檢查的密碼</param>
    /// <returns>如果有重複模式則返回true</returns>
    private static bool HasRepeatingPatterns(string password)
    {
        // 檢查連續重複字符（如：aaa, 111）
        for (int i = 0; i < password.Length - 2; i++)
        {
            if (password[i] == password[i + 1] && password[i + 1] == password[i + 2])
                return true;
        }

        // 檢查簡單的遞增/遞減序列（如：123, abc, 321）
        for (int i = 0; i < password.Length - 2; i++)
        {
            var char1 = password[i];
            var char2 = password[i + 1];
            var char3 = password[i + 2];

            if ((char2 == char1 + 1 && char3 == char2 + 1) ||
                (char2 == char1 - 1 && char3 == char2 - 1))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 產生密碼強度詳細報告
    /// </summary>
    /// <param name="password">要評估的密碼</param>
    /// <returns>密碼強度評估結果</returns>
    public PasswordStrengthResult EvaluatePasswordStrength(string password)
    {
        var result = new PasswordStrengthResult
        {
            Password = password,
            Score = GetPasswordStrength(password),
            IsStrong = IsPasswordStrong(password)
        };

        if (string.IsNullOrWhiteSpace(password))
        {
            result.Weaknesses.Add("密碼不能為空");
            return result;
        }

        if (password.Length < 8)
            result.Weaknesses.Add("密碼長度至少需要8個字符");

        if (!password.Any(char.IsUpper))
            result.Weaknesses.Add("需要包含大寫字母");

        if (!password.Any(char.IsLower))
            result.Weaknesses.Add("需要包含小寫字母");

        if (!password.Any(char.IsDigit))
            result.Weaknesses.Add("需要包含數字");

        if (!password.Any(c => !char.IsLetterOrDigit(c)))
            result.Weaknesses.Add("需要包含特殊字符");

        if (IsCommonWeakPassword(password))
            result.Weaknesses.Add("這是常見的弱密碼");

        if (HasRepeatingPatterns(password))
            result.Weaknesses.Add("包含重複模式");

        return result;
    }

    /// <summary>
    /// 安全地清除記憶體中的敏感字符串
    /// </summary>
    /// <param name="sensitive">要清除的敏感字符串</param>
    public static void SecureClearString(string sensitive)
    {
        if (string.IsNullOrEmpty(sensitive))
            return;

        unsafe
        {
            fixed (char* ptr = sensitive)
            {
                for (int i = 0; i < sensitive.Length; i++)
                {
                    ptr[i] = '\0';
                }
            }
        }
    }
}

/// <summary>
/// 密碼強度評估結果
/// </summary>
public class PasswordStrengthResult
{
    public string Password { get; set; } = string.Empty;
    public int Score { get; set; }
    public bool IsStrong { get; set; }
    public List<string> Weaknesses { get; set; } = new();
    
    public string GetStrengthDescription()
    {
        return Score switch
        {
            0 => "非常弱",
            1 => "弱",
            2 => "較弱",
            3 => "中等",
            4 => "較強",
            5 => "強",
            _ => "未知"
        };
    }
}