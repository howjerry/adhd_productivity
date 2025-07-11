using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace AdhdProductivitySystem.Infrastructure.Security;

/// <summary>
/// Service for validating security configuration at startup
/// </summary>
public class SecurityValidationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SecurityValidationService> _logger;

    // List of insecure default secrets that should never be used in production
    private static readonly HashSet<string> InsecureDefaults = new()
    {
        "ADHD_SuperSecret_Key_2024_ProductivitySystem_SecureToken",
        "YourVeryLongAndSecureSecretKeyThatIsAtLeast32CharactersLong!@#$%^&*()",
        "your_very_long_and_secure_jwt_secret_key_at_least_32_characters_long",
        "ADHD_Production_JWT_Secret_Key_2024_Very_Secure_And_Long_String_For_Container_Deployment",
        "adhd_secure_pass_2024",
        "admin_secure_pass_2024",
        "your_secure_db_password",
        "your_secure_admin_password",
        "password",
        "123456",
        "admin",
        "test"
    };

    private static readonly HashSet<string> InsecurePatterns = new()
    {
        "password",
        "123456",
        "admin",
        "test",
        "demo",
        "default",
        "secret",
        "key",
        "token"
    };

    public SecurityValidationService(IConfiguration configuration, ILogger<SecurityValidationService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Validates security configuration and throws exceptions if insecure defaults are found
    /// </summary>
    public void ValidateSecurityConfiguration()
    {
        var validationErrors = new List<string>();

        // Validate JWT Secret
        var jwtSecret = GetJwtSecret();
        if (string.IsNullOrEmpty(jwtSecret))
        {
            validationErrors.Add("JWT_SECRET_KEY environment variable is not set");
        }
        else
        {
            ValidateJwtSecret(jwtSecret, validationErrors);
        }

        // Validate Database Password
        var dbPassword = GetDatabasePassword();
        if (string.IsNullOrEmpty(dbPassword))
        {
            validationErrors.Add("POSTGRES_PASSWORD environment variable is not set");
        }
        else
        {
            ValidateDatabasePassword(dbPassword, validationErrors);
        }

        // Validate PgAdmin Password (if used)
        var pgAdminPassword = _configuration["PGADMIN_DEFAULT_PASSWORD"];
        if (!string.IsNullOrEmpty(pgAdminPassword))
        {
            ValidatePgAdminPassword(pgAdminPassword, validationErrors);
        }

        // Validate environment-specific settings
        var environment = _configuration["ASPNETCORE_ENVIRONMENT"];
        if (environment == "Production")
        {
            ValidateProductionSettings(validationErrors);
        }

        // Throw exception if any validation errors found
        if (validationErrors.Any())
        {
            var errorMessage = "Security validation failed:\n" + string.Join("\n", validationErrors.Select(e => $"- {e}"));
            _logger.LogCritical("Security validation failed: {ValidationErrors}", validationErrors);
            throw new InvalidOperationException(errorMessage);
        }

        _logger.LogInformation("Security validation passed successfully");
    }

    private string? GetJwtSecret()
    {
        return _configuration["JWT_SECRET_KEY"] ?? 
               _configuration["JWT:SecretKey"] ?? 
               _configuration["JwtSettings:Secret"];
    }

    private string? GetDatabasePassword()
    {
        return _configuration["POSTGRES_PASSWORD"];
    }

    private void ValidateJwtSecret(string jwtSecret, List<string> validationErrors)
    {
        // Check if it's a known insecure default
        if (IsInsecureDefault(jwtSecret))
        {
            validationErrors.Add("JWT secret is using a default/example value. Generate a secure secret.");
        }

        // Check minimum length
        if (jwtSecret.Length < 32)
        {
            validationErrors.Add("JWT secret must be at least 32 characters long");
        }

        // Check complexity
        if (!HasSufficientComplexity(jwtSecret))
        {
            validationErrors.Add("JWT secret must contain uppercase, lowercase, numbers, and special characters");
        }

        // Check entropy
        if (!HasSufficientEntropy(jwtSecret))
        {
            validationErrors.Add("JWT secret appears to have low entropy. Use a random generator.");
        }
    }

    private void ValidateDatabasePassword(string dbPassword, List<string> validationErrors)
    {
        if (IsInsecureDefault(dbPassword))
        {
            validationErrors.Add("Database password is using a default/example value");
        }

        if (dbPassword.Length < 12)
        {
            validationErrors.Add("Database password must be at least 12 characters long");
        }

        if (!HasSufficientComplexity(dbPassword))
        {
            validationErrors.Add("Database password must contain uppercase, lowercase, numbers, and special characters");
        }
    }

    private void ValidatePgAdminPassword(string pgAdminPassword, List<string> validationErrors)
    {
        if (IsInsecureDefault(pgAdminPassword))
        {
            validationErrors.Add("PgAdmin password is using a default/example value");
        }

        if (pgAdminPassword.Length < 8)
        {
            validationErrors.Add("PgAdmin password must be at least 8 characters long");
        }
    }

    private void ValidateProductionSettings(List<string> validationErrors)
    {
        // In production, certain settings must be properly configured
        var allowedHosts = _configuration["AllowedHosts"];
        if (allowedHosts == "*")
        {
            validationErrors.Add("AllowedHosts should not be '*' in production");
        }

        // Check CORS settings
        var corsOrigins = _configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (corsOrigins?.Any(origin => origin.Contains("localhost")) == true)
        {
            validationErrors.Add("CORS should not allow localhost origins in production");
        }

        // Check logging level
        var logLevel = _configuration["Logging:LogLevel:Default"];
        if (logLevel == "Debug" || logLevel == "Trace")
        {
            validationErrors.Add("Log level should not be Debug or Trace in production");
        }
    }

    private static bool IsInsecureDefault(string value)
    {
        if (InsecureDefaults.Contains(value))
        {
            return true;
        }

        // Check for patterns that suggest default/test values
        var lowerValue = value.ToLowerInvariant();
        return InsecurePatterns.Any(pattern => lowerValue.Contains(pattern) && 
                                              (lowerValue.Contains("default") || 
                                               lowerValue.Contains("example") ||
                                               lowerValue.Contains("test") ||
                                               lowerValue.Contains("demo")));
    }

    private static bool HasSufficientComplexity(string value)
    {
        var hasUpper = value.Any(char.IsUpper);
        var hasLower = value.Any(char.IsLower);
        var hasDigit = value.Any(char.IsDigit);
        var hasSpecial = value.Any(ch => !char.IsLetterOrDigit(ch));

        return hasUpper && hasLower && hasDigit && hasSpecial;
    }

    private static bool HasSufficientEntropy(string value)
    {
        // Simple entropy check - count unique characters
        var uniqueChars = value.Distinct().Count();
        var entropy = uniqueChars / (double)value.Length;
        
        // Should have at least 50% unique characters for sufficient entropy
        return entropy >= 0.5 && uniqueChars >= 16;
    }
}

/// <summary>
/// Extension methods for registering security validation
/// </summary>
public static class SecurityValidationExtensions
{
    /// <summary>
    /// Adds security validation services and performs validation
    /// </summary>
    public static IServiceCollection AddSecurityValidation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<SecurityValidationService>();
        
        // Perform validation at startup
        var serviceProvider = services.BuildServiceProvider();
        var validationService = serviceProvider.GetRequiredService<SecurityValidationService>();
        validationService.ValidateSecurityConfiguration();
        
        return services;
    }
}