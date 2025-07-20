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

namespace AdhdProductivitySystem.Tests
{
    public class SimpleAuthTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordService _passwordService;
        private readonly JwtService _jwtService;

        public SimpleAuthTests()
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

        [Fact]
        public void PasswordService_HashPassword_ShouldWork()
        {
            // Arrange
            var password = "TestPassword123!";

            // Act
            var (hash, salt) = _passwordService.HashPassword(password);

            // Assert
            hash.Should().NotBeNullOrEmpty();
            salt.Should().NotBeNullOrEmpty();
            _passwordService.VerifyPassword(password, hash, salt).Should().BeTrue();
        }

        [Fact]
        public void PasswordService_IsPasswordStrong_ShouldValidateCorrectly()
        {
            // Arrange & Act & Assert
            _passwordService.IsPasswordStrong("Password123!").Should().BeTrue();
            _passwordService.IsPasswordStrong("weak").Should().BeFalse();
            _passwordService.IsPasswordStrong("password").Should().BeFalse();
        }

        [Fact]
        public void JwtService_GenerateToken_ShouldCreateValidToken()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                Name = "Test User",
                PasswordHash = "hash",
                PasswordSalt = "salt"
            };

            // Act
            var (token, expiresAt) = _jwtService.GenerateToken(user);

            // Assert
            token.Should().NotBeNullOrEmpty();
            expiresAt.Should().BeAfter(DateTime.UtcNow);
        }

        [Fact]
        public void JwtService_ValidateToken_ShouldWork()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                Name = "Test User",
                PasswordHash = "hash",
                PasswordSalt = "salt"
            };

            var (token, _) = _jwtService.GenerateToken(user);

            // Act
            var principal = _jwtService.ValidateToken(token);

            // Assert
            principal.Should().NotBeNull();
        }

        [Fact]
        public async Task RefreshToken_BasicFunctionality_ShouldWork()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                Name = "Test User",
                PasswordHash = "hash",
                PasswordSalt = "salt"
            };
            
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

            // Assert
            savedToken.IsValid.Should().BeTrue();
            savedToken.UserId.Should().Be(user.Id);
        }

        [Fact]
        public async Task RefreshToken_Revoke_ShouldWork()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                Name = "Test User",
                PasswordHash = "hash",
                PasswordSalt = "salt"
            };
            
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
            savedToken.IsValid.Should().BeFalse();
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}