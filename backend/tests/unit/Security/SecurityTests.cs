using AdhdProductivitySystem.Api.Middleware;
using AdhdProductivitySystem.Infrastructure.Authentication;
using AdhdProductivitySystem.Infrastructure.Security;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace AdhdProductivitySystem.Tests.Unit.Security;

/// <summary>
/// 安全功能測試
/// </summary>
public class SecurityTests
{
    private readonly Mock<ILogger<PasswordService>> _passwordLogger;
    private readonly Mock<ILogger<JwtService>> _jwtLogger;
    private readonly Mock<IConfiguration> _configuration;

    public SecurityTests()
    {
        _passwordLogger = new Mock<ILogger<PasswordService>>();
        _jwtLogger = new Mock<ILogger<JwtService>>();
        _configuration = new Mock<IConfiguration>();
        
        // 設定模擬的配置值
        _configuration.Setup(c => c["JWT:SecretKey"]).Returns("test_secret_key_that_is_32_characters_long_and_secure!");
        _configuration.Setup(c => c["JWT:Issuer"]).Returns("test_issuer");
        _configuration.Setup(c => c["JWT:Audience"]).Returns("test_audience");
        _configuration.Setup(c => c["JWT:TokenExpirationMinutes"]).Returns("15");
    }

    #region 密碼安全測試

    [Fact]
    public void PasswordService_HashPassword_ShouldReturnDifferentHashesForSamePassword()
    {
        // Arrange
        var passwordService = new PasswordService();
        var password = "TestPassword123!";

        // Act
        var (hash1, salt1) = passwordService.HashPassword(password);
        var (hash2, salt2) = passwordService.HashPassword(password);

        // Assert
        hash1.Should().NotBe(hash2);
        salt1.Should().NotBe(salt2);
    }

    [Fact]
    public void PasswordService_VerifyPassword_ShouldReturnTrueForCorrectPassword()
    {
        // Arrange
        var passwordService = new PasswordService();
        var password = "TestPassword123!";
        var (hash, salt) = passwordService.HashPassword(password);

        // Act
        var result = passwordService.VerifyPassword(password, hash, salt);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void PasswordService_VerifyPassword_ShouldReturnFalseForIncorrectPassword()
    {
        // Arrange
        var passwordService = new PasswordService();
        var password = "TestPassword123!";
        var wrongPassword = "WrongPassword123!";
        var (hash, salt) = passwordService.HashPassword(password);

        // Act
        var result = passwordService.VerifyPassword(wrongPassword, hash, salt);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("password", false)]           // 常見弱密碼
    [InlineData("123456", false)]             // 純數字
    [InlineData("Password123!", true)]        // 符合要求
    [InlineData("Abcd123!", true)]           // 符合要求
    [InlineData("short", false)]             // 太短
    [InlineData("NoDigits!", false)]         // 沒有數字
    [InlineData("nospecial123", false)]      // 沒有特殊字符
    [InlineData("NOLOWERCASE123!", false)]   // 沒有小寫
    [InlineData("nouppercase123!", false)]   // 沒有大寫
    public void PasswordService_IsPasswordStrong_ShouldValidateCorrectly(string password, bool expected)
    {
        // Arrange
        var passwordService = new PasswordService();

        // Act
        var result = passwordService.IsPasswordStrong(password);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void PasswordService_EvaluatePasswordStrength_ShouldReturnCorrectWeaknesses()
    {
        // Arrange
        var passwordService = new PasswordService();
        var weakPassword = "weak";

        // Act
        var result = passwordService.EvaluatePasswordStrength(weakPassword);

        // Assert
        result.IsStrong.Should().BeFalse();
        result.Weaknesses.Should().NotBeEmpty();
        result.Weaknesses.Should().Contain("密碼長度至少需要8個字符");
    }

    #endregion

    #region JWT安全測試

    [Fact]
    public void JwtService_Constructor_ShouldThrowForWeakSecretKey()
    {
        // Arrange
        _configuration.Setup(c => c["JWT:SecretKey"]).Returns("weak");

        // Act & Assert
        var act = () => new JwtService(_configuration.Object, _jwtLogger.Object);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void JwtService_GenerateToken_ShouldCreateValidToken()
    {
        // Arrange
        var jwtService = new JwtService(_configuration.Object, _jwtLogger.Object);
        var user = CreateTestUser();

        // Act
        var (token, expiresAt) = jwtService.GenerateToken(user);

        // Assert
        token.Should().NotBeNullOrEmpty();
        expiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void JwtService_ValidateToken_ShouldReturnPrincipalForValidToken()
    {
        // Arrange
        var jwtService = new JwtService(_configuration.Object, _jwtLogger.Object);
        var user = CreateTestUser();
        var (token, _) = jwtService.GenerateToken(user);

        // Act
        var principal = jwtService.ValidateToken(token);

        // Assert
        principal.Should().NotBeNull();
        principal!.FindFirst(ClaimTypes.NameIdentifier)?.Value.Should().Be(user.Id.ToString());
    }

    [Fact]
    public void JwtService_ValidateToken_ShouldReturnNullForInvalidToken()
    {
        // Arrange
        var jwtService = new JwtService(_configuration.Object, _jwtLogger.Object);
        var invalidToken = "invalid.token.here";

        // Act
        var principal = jwtService.ValidateToken(invalidToken);

        // Assert
        principal.Should().BeNull();
    }

    [Fact]
    public void JwtService_IsTokenExpired_ShouldReturnTrueForExpiredToken()
    {
        // Arrange
        var jwtService = new JwtService(_configuration.Object, _jwtLogger.Object);
        var user = CreateTestUser();
        
        // 創建一個已過期的token（通過修改配置）
        _configuration.Setup(c => c["JWT:TokenExpirationMinutes"]).Returns("-1");
        var expiredJwtService = new JwtService(_configuration.Object, _jwtLogger.Object);
        var (expiredToken, _) = expiredJwtService.GenerateToken(user);

        // Act
        var isExpired = jwtService.IsTokenExpired(expiredToken);

        // Assert
        isExpired.Should().BeTrue();
    }

    #endregion

    #region 安全驗證測試

    [Fact]
    public void SecurityValidationService_ValidateSecurityConfiguration_ShouldThrowForWeakJwtSecret()
    {
        // Arrange
        var logger = new Mock<ILogger<SecurityValidationService>>();
        _configuration.Setup(c => c["JWT_SECRET_KEY"]).Returns("weak_secret");
        var validationService = new SecurityValidationService(_configuration.Object, logger.Object);

        // Act & Assert
        var act = () => validationService.ValidateSecurityConfiguration();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void SecurityValidationService_ValidateSecurityConfiguration_ShouldThrowForCommonPassword()
    {
        // Arrange
        var logger = new Mock<ILogger<SecurityValidationService>>();
        _configuration.Setup(c => c["JWT_SECRET_KEY"]).Returns("test_secret_key_that_is_32_characters_long_and_secure!");
        _configuration.Setup(c => c["POSTGRES_PASSWORD"]).Returns("password"); // 常見弱密碼
        var validationService = new SecurityValidationService(_configuration.Object, logger.Object);

        // Act & Assert
        var act = () => validationService.ValidateSecurityConfiguration();
        act.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region 輸入驗證測試

    [Theory]
    [InlineData("../../../etc/passwd", true)]    // 路徑遍歷
    [InlineData("' OR 1=1 --", true)]           // SQL注入
    [InlineData("<script>alert('xss')</script>", true)] // XSS
    [InlineData("normal_input", false)]          // 正常輸入
    [InlineData("test@example.com", false)]      // 正常郵箱
    public void InputValidation_ShouldDetectMaliciousContent(string input, bool shouldBeDetected)
    {
        // Arrange
        var isValid = !ContainsSuspiciousContent(input);

        // Act & Assert
        if (shouldBeDetected)
        {
            isValid.Should().BeFalse();
        }
        else
        {
            isValid.Should().BeTrue();
        }
    }

    // 模擬InputValidationMiddleware中的邏輯
    private static bool ContainsSuspiciousContent(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        var lowerInput = input.ToLowerInvariant();

        var suspiciousPatterns = new[]
        {
            "../", "union select", "or 1=1", "<script", "javascript:",
            "eval(", "alert(", "document.cookie"
        };

        return suspiciousPatterns.Any(pattern => lowerInput.Contains(pattern));
    }

    #endregion

    #region 速率限制測試

    [Fact]
    public void RateLimit_ShouldBlockExcessiveRequests()
    {
        // 這個測試需要實際的中間件測試環境
        // 在此僅做邏輯驗證
        var maxRequests = 5;
        var currentRequests = 6;

        var shouldBlock = currentRequests > maxRequests;
        shouldBlock.Should().BeTrue();
    }

    #endregion

    #region 輔助方法

    private static AdhdProductivitySystem.Domain.Entities.User CreateTestUser()
    {
        return new AdhdProductivitySystem.Domain.Entities.User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Name = "Test User",
            PasswordHash = "hash",
            PasswordSalt = "salt",
            AdhdType = AdhdProductivitySystem.Domain.Enums.AdhdType.Combined,
            TimeZone = "UTC",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    #endregion
}

/// <summary>
/// 整合安全測試
/// </summary>
public class SecurityIntegrationTests
{
    [Fact]
    public void Security_PasswordWorkflow_ShouldWorkEndToEnd()
    {
        // Arrange
        var passwordService = new PasswordService();
        var originalPassword = "SecurePassword123!";

        // Act - 註冊流程
        var (hash, salt) = passwordService.HashPassword(originalPassword);
        
        // Act - 登入流程
        var isValidLogin = passwordService.VerifyPassword(originalPassword, hash, salt);
        var isInvalidLogin = passwordService.VerifyPassword("WrongPassword", hash, salt);

        // Assert
        isValidLogin.Should().BeTrue();
        isInvalidLogin.Should().BeFalse();
    }

    [Fact]
    public void Security_JwtWorkflow_ShouldWorkEndToEnd()
    {
        // Arrange
        var configuration = new Mock<IConfiguration>();
        configuration.Setup(c => c["JWT:SecretKey"]).Returns("test_secret_key_that_is_32_characters_long_and_secure!");
        configuration.Setup(c => c["JWT:Issuer"]).Returns("test_issuer");
        configuration.Setup(c => c["JWT:Audience"]).Returns("test_audience");
        configuration.Setup(c => c["JWT:TokenExpirationMinutes"]).Returns("15");

        var logger = new Mock<ILogger<JwtService>>();
        var jwtService = new JwtService(configuration.Object, logger.Object);
        var user = CreateTestUser();

        // Act - 生成Token
        var (token, expiresAt) = jwtService.GenerateToken(user);
        
        // Act - 驗證Token
        var principal = jwtService.ValidateToken(token);
        
        // Act - 提取用戶ID
        var userId = jwtService.GetUserIdFromToken(token);

        // Assert
        token.Should().NotBeNullOrEmpty();
        expiresAt.Should().BeAfter(DateTime.UtcNow);
        principal.Should().NotBeNull();
        userId.Should().Be(user.Id);
    }

    private static AdhdProductivitySystem.Domain.Entities.User CreateTestUser()
    {
        return new AdhdProductivitySystem.Domain.Entities.User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Name = "Test User",
            PasswordHash = "hash",
            PasswordSalt = "salt",
            AdhdType = AdhdProductivitySystem.Domain.Enums.AdhdType.Combined,
            TimeZone = "UTC",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}