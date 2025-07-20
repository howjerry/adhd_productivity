using System;
using System.Threading.Tasks;
using AdhdProductivitySystem.Domain.Entities;
using AdhdProductivitySystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FluentAssertions;

namespace AdhdProductivitySystem.Tests
{
    public class RefreshTokenTests : IDisposable
    {
        private readonly ApplicationDbContext _context;

        public RefreshTokenTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();
            
            // 創建測試用戶
            var testUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                Name = "Test User",
                PasswordHash = "hash",
                PasswordSalt = "salt"
            };
            _context.Users.Add(testUser);
            _context.SaveChanges();
        }

        [Fact]
        public async Task RefreshToken_IsValid_ReturnsTrueWhenNotRevokedAndNotExpired()
        {
            // Arrange
            var user = await _context.Users.FirstAsync();
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
        }

        [Fact]
        public async Task RefreshToken_IsValid_ReturnsFalseWhenRevoked()
        {
            // Arrange
            var user = await _context.Users.FirstAsync();
            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = Guid.NewGuid().ToString(),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = true,
                RevokedAt = DateTime.UtcNow
            };
            
            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            // Act
            var savedToken = await _context.RefreshTokens.FirstAsync();

            // Assert
            savedToken.IsValid.Should().BeFalse();
        }

        [Fact]
        public async Task RefreshToken_IsValid_ReturnsFalseWhenExpired()
        {
            // Arrange
            var user = await _context.Users.FirstAsync();
            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = Guid.NewGuid().ToString(),
                ExpiresAt = DateTime.UtcNow.AddDays(-1), // 昨天過期
                IsRevoked = false
            };
            
            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            // Act
            var savedToken = await _context.RefreshTokens.FirstAsync();

            // Assert
            savedToken.IsValid.Should().BeFalse();
        }

        [Fact]
        public async Task RefreshToken_Revoke_SetsRevokedFlagAndTimestamp()
        {
            // Arrange
            var user = await _context.Users.FirstAsync();
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
        }

        [Fact]
        public async Task RefreshToken_UniqueIndex_PreventsduplicateTokens()
        {
            // Arrange
            var user = await _context.Users.FirstAsync();
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

            // Act
            _context.RefreshTokens.Add(refreshToken1);
            await _context.SaveChangesAsync();
            
            _context.RefreshTokens.Add(refreshToken2);

            // Assert
            await Assert.ThrowsAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}