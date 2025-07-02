using AdhdProductivitySystem.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AdhdProductivitySystem.Infrastructure.Authentication;

/// <summary>
/// Service for handling JWT token operations with enhanced security
/// </summary>
public class JwtService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtService> _logger;
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _tokenExpirationMinutes;
    private readonly int _refreshTokenExpirationDays;

    public JwtService(IConfiguration configuration, ILogger<JwtService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        // Get JWT configuration with fallback support
        _secretKey = GetJwtSecret() ?? throw new ArgumentNullException("JWT:SecretKey not configured");
        _issuer = GetJwtIssuer() ?? throw new ArgumentNullException("JWT:Issuer not configured");
        _audience = GetJwtAudience() ?? throw new ArgumentNullException("JWT:Audience not configured");
        _tokenExpirationMinutes = GetJwtExpirationMinutes();
        _refreshTokenExpirationDays = GetRefreshTokenExpirationDays();
        
        // Validate secret key strength
        ValidateSecretKey(_secretKey);
    }

    private string? GetJwtSecret()
    {
        return _configuration["JWT_SECRET_KEY"] ?? 
               _configuration["JWT:SecretKey"] ?? 
               _configuration["JwtSettings:Secret"];
    }

    private string? GetJwtIssuer()
    {
        return _configuration["JWT_ISSUER"] ?? 
               _configuration["JWT:Issuer"] ?? 
               _configuration["JwtSettings:Issuer"];
    }

    private string? GetJwtAudience()
    {
        return _configuration["JWT_AUDIENCE"] ?? 
               _configuration["JWT:Audience"] ?? 
               _configuration["JwtSettings:Audience"];
    }

    private int GetJwtExpirationMinutes()
    {
        var configValue = _configuration["JWT_EXPIRY_MINUTES"] ?? 
                         _configuration["JWT:TokenExpirationMinutes"] ?? 
                         _configuration["JwtSettings:ExpiryMinutes"] ?? "60";
        
        return int.TryParse(configValue, out var minutes) ? minutes : 60;
    }

    private int GetRefreshTokenExpirationDays()
    {
        var configValue = _configuration["JWT_REFRESH_EXPIRY_DAYS"] ?? "7";
        return int.TryParse(configValue, out var days) ? days : 7;
    }

    private static void ValidateSecretKey(string secretKey)
    {
        if (string.IsNullOrEmpty(secretKey))
            throw new ArgumentException("JWT secret key cannot be null or empty");

        if (secretKey.Length < 32)
            throw new ArgumentException("JWT secret key must be at least 32 characters long");

        // Check for common weak patterns
        var lowerKey = secretKey.ToLowerInvariant();
        if (lowerKey.Contains("secret") || lowerKey.Contains("key") || lowerKey.Contains("password") || 
            lowerKey.Contains("token") || lowerKey.Contains("test") || lowerKey.Contains("example"))
        {
            throw new ArgumentException("JWT secret key appears to contain weak patterns. Use a cryptographically secure random string.");
        }
    }

    /// <summary>
    /// Generates a JWT token for the specified user
    /// </summary>
    /// <param name="user">User to generate token for</param>
    /// <param name="deviceId">Optional device identifier</param>
    /// <returns>JWT token with expiration information</returns>
    public (string Token, DateTime ExpiresAt) GenerateToken(User user, string? deviceId = null)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secretKey);
            var expiresAt = DateTime.UtcNow.AddMinutes(_tokenExpirationMinutes);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Name, user.Name),
                new("adhd_type", user.AdhdType.ToString()),
                new("timezone", user.TimeZone),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // Unique token ID
                new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new("token_type", "access"),
                new("user_version", user.UpdatedAt.Ticks.ToString()) // For invalidating tokens when user data changes
            };

            // Add device information if provided
            if (!string.IsNullOrEmpty(deviceId))
            {
                claims.Add(new("device_id", deviceId));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expiresAt,
                NotBefore = DateTime.UtcNow,
                IssuedAt = DateTime.UtcNow,
                Issuer = _issuer,
                Audience = _audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            _logger.LogDebug("Generated JWT token for user {UserId} with expiration {ExpiresAt}", user.Id, expiresAt);
            return (tokenString, expiresAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating JWT token for user {UserId}", user.Id);
            throw;
        }
    }

    /// <summary>
    /// Generates a cryptographically secure refresh token
    /// </summary>
    /// <returns>Refresh token with expiration information</returns>
    public (string Token, DateTime ExpiresAt) GenerateRefreshToken()
    {
        try
        {
            var randomNumber = new byte[64]; // Increased size for better security
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            
            var token = Convert.ToBase64String(randomNumber);
            var expiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays);
            
            _logger.LogDebug("Generated refresh token with expiration {ExpiresAt}", expiresAt);
            return (token, expiresAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating refresh token");
            throw;
        }
    }

    /// <summary>
    /// Validates a JWT token and returns the principal with detailed validation
    /// </summary>
    /// <param name="token">JWT token to validate</param>
    /// <param name="validateLifetime">Whether to validate token expiration</param>
    /// <returns>Claims principal if valid, null otherwise</returns>
    public ClaimsPrincipal? ValidateToken(string token, bool validateLifetime = true)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Token validation failed: Token is null or empty");
                return null;
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = validateLifetime,
                RequireExpirationTime = true,
                RequireSignedTokens = true,
                ClockSkew = TimeSpan.FromMinutes(2) // Allow small clock skew
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            
            // Additional validation for JWT tokens
            if (validatedToken is not JwtSecurityToken jwt)
            {
                _logger.LogWarning("Token validation failed: Invalid token type");
                return null;
            }

            // Validate algorithm
            if (!jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                _logger.LogWarning("Token validation failed: Invalid signing algorithm {Algorithm}", jwt.Header.Alg);
                return null;
            }

            // Check token type claim if present
            var tokenTypeClaim = principal.FindFirst("token_type")?.Value;
            if (!string.IsNullOrEmpty(tokenTypeClaim) && tokenTypeClaim != "access")
            {
                _logger.LogWarning("Token validation failed: Invalid token type {TokenType}", tokenTypeClaim);
                return null;
            }

            _logger.LogDebug("Token validated successfully for user {UserId}", principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            return principal;
        }
        catch (SecurityTokenExpiredException ex)
        {
            _logger.LogDebug("Token validation failed: Token expired. {Message}", ex.Message);
            return null;
        }
        catch (SecurityTokenInvalidSignatureException ex)
        {
            _logger.LogWarning("Token validation failed: Invalid signature. {Message}", ex.Message);
            return null;
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning("Token validation failed: Security token exception. {Message}", ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token validation");
            return null;
        }
    }

    /// <summary>
    /// Extracts user ID from JWT token
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>User ID if valid, null otherwise</returns>
    public Guid? GetUserIdFromToken(string token)
    {
        var principal = ValidateToken(token);
        if (principal == null) return null;

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return null;

        return userId;
    }

    /// <summary>
    /// Checks if a token is expired
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>True if expired, false otherwise</returns>
    public bool IsTokenExpired(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonToken = tokenHandler.ReadJwtToken(token);
            return jsonToken.ValidTo < DateTime.UtcNow;
        }
        catch
        {
            return true;
        }
    }
}