using System;
using System.Threading.Tasks;
using AdhdProductivitySystem.Domain.Entities;
using AdhdProductivitySystem.Infrastructure.Authentication;
using AdhdProductivitySystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace SecurityTests
{
    /// <summary>
    /// 認證與安全系統驗證測試
    /// </summary>
    public class AuthSecurityTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordService _passwordService;
        private readonly JwtService _jwtService;

        public AuthSecurityTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();
            
            _passwordService = new PasswordService();
            
            // 設定 JWT 配置
            var configuration = new Mock<IConfiguration>();
            configuration.Setup(c => c["JWT:SecretKey"]).Returns("test_secret_key_that_is_32_characters_long_and_secure!");
            configuration.Setup(c => c["JWT:Issuer"]).Returns("test_issuer");
            configuration.Setup(c => c["JWT:Audience"]).Returns("test_audience");
            configuration.Setup(c => c["JWT:TokenExpirationMinutes"]).Returns("15");
            
            var logger = new Mock<ILogger<JwtService>>();
            _jwtService = new JwtService(configuration.Object, logger.Object);
        }

        #region 密碼安全測試

        [Fact]
        public void PasswordService_HashPassword_應該產生不同的Hash()
        {
            // Arrange
            var password = "TestPassword123!";

            // Act
            var (hash1, salt1) = _passwordService.HashPassword(password);
            var (hash2, salt2) = _passwordService.HashPassword(password);

            // Assert
            hash1.Should().NotBeNullOrEmpty();
            salt1.Should().NotBeNullOrEmpty();
            hash1.Should().NotBe(hash2);
            salt1.Should().NotBe(salt2);
        }

        [Fact]
        public void PasswordService_VerifyPassword_應該正確驗證()
        {
            // Arrange
            var password = "TestPassword123!";
            var (hash, salt) = _passwordService.HashPassword(password);

            // Act & Assert
            _passwordService.VerifyPassword(password, hash, salt).Should().BeTrue();
            _passwordService.VerifyPassword("錯誤密碼", hash, salt).Should().BeFalse();
        }

        [Theory]
        [InlineData("Password123!", true)]        // 符合要求
        [InlineData("Abcd123!", true)]           // 符合要求
        [InlineData("password", false)]          // 常見弱密碼
        [InlineData("123456", false)]            // 純數字
        [InlineData("short", false)]             // 太短
        [InlineData("NoDigits!", false)]         // 沒有數字
        [InlineData("nospecial123", false)]      // 沒有特殊字符
        [InlineData("NOLOWERCASE123!", false)]   // 沒有小寫
        [InlineData("nouppercase123!", false)]   // 沒有大寫
        public void PasswordService_IsPasswordStrong_應該正確驗證密碼強度(string password, bool expected)
        {
            // Act
            var result = _passwordService.IsPasswordStrong(password);

            // Assert
            result.Should().Be(expected);
        }

        #endregion

        #region JWT 安全測試

        [Fact]
        public void JwtService_GenerateToken_應該創建有效Token()
        {
            // Arrange
            var user = CreateTestUser();

            // Act
            var (token, expiresAt) = _jwtService.GenerateToken(user);

            // Assert
            token.Should().NotBeNullOrEmpty();
            expiresAt.Should().BeAfter(DateTime.UtcNow);
        }

        [Fact]
        public void JwtService_ValidateToken_應該正確驗證有效Token()
        {
            // Arrange
            var user = CreateTestUser();
            var (token, _) = _jwtService.GenerateToken(user);

            // Act
            var principal = _jwtService.ValidateToken(token);

            // Assert
            principal.Should().NotBeNull();
            principal!.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value.Should().Be(user.Id.ToString());
        }

        [Fact]
        public void JwtService_ValidateToken_應該拒絕無效Token()
        {
            // Act
            var principal = _jwtService.ValidateToken("invalid.token.here");

            // Assert
            principal.Should().BeNull();
        }

        #endregion

        #region RefreshToken 持久化測試

        [Fact]
        public async Task RefreshToken_儲存到資料庫_應該成功()
        {
            // Arrange
            var user = CreateTestUser();
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = Guid.NewGuid().ToString(),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            };
            
            // Act
            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            // Assert
            var savedToken = await _context.RefreshTokens.FirstAsync();
            savedToken.Should().NotBeNull();
            savedToken.UserId.Should().Be(user.Id);
            savedToken.IsValid.Should().BeTrue();
        }

        [Fact]
        public async Task RefreshToken_IsValid_過期Token應該無效()
        {
            // Arrange
            var user = CreateTestUser();
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var expiredToken = new RefreshToken
            {
                UserId = user.Id,
                Token = Guid.NewGuid().ToString(),
                ExpiresAt = DateTime.UtcNow.AddDays(-1), // 昨天過期
                IsRevoked = false
            };
            
            // Act
            _context.RefreshTokens.Add(expiredToken);
            await _context.SaveChangesAsync();

            // Assert
            var savedToken = await _context.RefreshTokens.FirstAsync();
            savedToken.IsValid.Should().BeFalse();
        }

        [Fact]
        public async Task RefreshToken_Revoke_應該正確撤銷()
        {
            // Arrange
            var user = CreateTestUser();
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = Guid.NewGuid().ToString(),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            };
            
            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            // Act
            var savedToken = await _context.RefreshTokens.FirstAsync();
            savedToken.Revoke();
            await _context.SaveChangesAsync();

            // Assert
            savedToken.IsRevoked.Should().BeTrue();
            savedToken.RevokedAt.Should().NotBeNull();
            savedToken.RevokedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            savedToken.IsValid.Should().BeFalse();
        }

        [Fact]
        public async Task RefreshToken_重複Token_應該防止()
        {
            // Arrange
            var user = CreateTestUser();
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var tokenValue = Guid.NewGuid().ToString();
            
            var refreshToken1 = new RefreshToken
            {
                UserId = user.Id,
                Token = tokenValue,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            };
            
            var refreshToken2 = new RefreshToken
            {
                UserId = user.Id,
                Token = tokenValue, // 相同的 token
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            };

            // Act & Assert
            _context.RefreshTokens.Add(refreshToken1);
            await _context.SaveChangesAsync();
            
            _context.RefreshTokens.Add(refreshToken2);
            await Assert.ThrowsAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());
        }

        #endregion

        #region 整合測試

        [Fact]
        public async Task 完整認證流程_應該正常運作()
        {
            // Arrange
            var originalPassword = "SecurePassword123!";
            var (hash, salt) = _passwordService.HashPassword(originalPassword);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                Name = "Test User",
                PasswordHash = hash,
                PasswordSalt = salt
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act - 模擬登入流程
            var isValidLogin = _passwordService.VerifyPassword(originalPassword, hash, salt);
            var (accessToken, _) = _jwtService.GenerateToken(user);
            var refreshTokenResult = _jwtService.GenerateRefreshToken();
            
            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshTokenResult.Token,
                ExpiresAt = refreshTokenResult.ExpiresAt,
                IsRevoked = false
            };
            
            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            // Act - 驗證流程
            var principal = _jwtService.ValidateToken(accessToken);
            var savedRefreshToken = await _context.RefreshTokens.FirstAsync(rt => rt.Token == refreshTokenResult.Token);

            // Assert
            isValidLogin.Should().BeTrue();
            accessToken.Should().NotBeNullOrEmpty();
            principal.Should().NotBeNull();
            savedRefreshToken.Should().NotBeNull();
            savedRefreshToken.IsValid.Should().BeTrue();
        }

        #endregion

        private static User CreateTestUser()
        {
            return new User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                Name = "Test User",
                PasswordHash = "hash",
                PasswordSalt = "salt"
            };
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}