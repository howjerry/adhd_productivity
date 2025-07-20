using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdhdProductivitySystem.Application.Common.Interfaces;
using AdhdProductivitySystem.Domain.Entities;
using AdhdProductivitySystem.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AdhdProductivitySystem.Application.IntegrationTests.Auth;

/// <summary>
/// Refresh Token 功能的完整整合測試
/// 測試從 Domain Entity 到 Repository 層的完整流程
/// </summary>
public class RefreshTokenIntegrationTests : IntegrationTestBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRepository<RefreshToken> _refreshTokenRepository;

    public RefreshTokenIntegrationTests()
    {
        _unitOfWork = ServiceProvider.GetRequiredService<IUnitOfWork>();
        _refreshTokenRepository = _unitOfWork.Repository<RefreshToken>();
    }

    #region 基本 CRUD 操作測試

    [Fact]
    public async Task CreateRefreshToken_WithValidData_ShouldSucceed()
    {
        // Arrange
        var refreshToken = CreateTestRefreshToken();

        // Act
        await _refreshTokenRepository.AddAsync(refreshToken);
        var result = await _unitOfWork.SaveChangesAsync();

        // Assert
        result.Should().BeGreaterThan(0);
        
        var savedToken = await _refreshTokenRepository.GetByIdAsync(refreshToken.Id);
        savedToken.Should().NotBeNull();
        savedToken!.Token.Should().Be(refreshToken.Token);
        savedToken.UserId.Should().Be(refreshToken.UserId);
        savedToken.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task GetRefreshToken_ByToken_ShouldReturnCorrectToken()
    {
        // Arrange
        var refreshToken = CreateTestRefreshToken();
        await _refreshTokenRepository.AddAsync(refreshToken);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var result = await _refreshTokenRepository.FirstOrDefaultAsync(
            rt => rt.Token == refreshToken.Token);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(refreshToken.Id);
        result.UserId.Should().Be(refreshToken.UserId);
    }

    [Fact]
    public async Task GetUserRefreshTokens_ShouldReturnOnlyUserTokens()
    {
        // Arrange
        var userId1 = "user1";
        var userId2 = "user2";
        
        var user1Tokens = new[]
        {
            CreateTestRefreshToken(userId1, "token1"),
            CreateTestRefreshToken(userId1, "token2")
        };
        
        var user2Token = CreateTestRefreshToken(userId2, "token3");

        await _refreshTokenRepository.AddRangeAsync(user1Tokens.Concat(new[] { user2Token }));
        await _unitOfWork.SaveChangesAsync();

        // Act
        var result = await _refreshTokenRepository.FindAsync(rt => rt.UserId == userId1);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(rt => rt.UserId == userId1);
    }

    #endregion

    #region Token 有效性測試

    [Fact]
    public async Task RefreshToken_WhenNotExpired_ShouldBeValid()
    {
        // Arrange
        var refreshToken = CreateTestRefreshToken(expiresAt: DateTime.UtcNow.AddDays(7));
        await _refreshTokenRepository.AddAsync(refreshToken);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var savedToken = await _refreshTokenRepository.GetByIdAsync(refreshToken.Id);

        // Assert
        savedToken!.IsValid.Should().BeTrue();
        savedToken.IsRevoked.Should().BeFalse();
        savedToken.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task RefreshToken_WhenExpired_ShouldBeInvalid()
    {
        // Arrange
        var refreshToken = CreateTestRefreshToken(expiresAt: DateTime.UtcNow.AddDays(-1));
        await _refreshTokenRepository.AddAsync(refreshToken);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var savedToken = await _refreshTokenRepository.GetByIdAsync(refreshToken.Id);

        // Assert
        savedToken!.IsValid.Should().BeFalse();
        savedToken.ExpiresAt.Should().BeBefore(DateTime.UtcNow);
    }

    [Fact]
    public async Task RefreshToken_WhenRevoked_ShouldBeInvalid()
    {
        // Arrange
        var refreshToken = CreateTestRefreshToken();
        refreshToken.Revoke();
        
        await _refreshTokenRepository.AddAsync(refreshToken);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var savedToken = await _refreshTokenRepository.GetByIdAsync(refreshToken.Id);

        // Assert
        savedToken!.IsValid.Should().BeFalse();
        savedToken.IsRevoked.Should().BeTrue();
        savedToken.RevokedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region Token 管理操作測試

    [Fact]
    public async Task RevokeRefreshToken_ShouldUpdateTokenAndSave()
    {
        // Arrange
        var refreshToken = CreateTestRefreshToken();
        await _refreshTokenRepository.AddAsync(refreshToken);
        await _unitOfWork.SaveChangesAsync();

        // Act
        refreshToken.Revoke();
        _refreshTokenRepository.Update(refreshToken);
        await _unitOfWork.SaveChangesAsync();

        // Assert
        var updatedToken = await _refreshTokenRepository.GetByIdAsync(refreshToken.Id);
        updatedToken!.IsRevoked.Should().BeTrue();
        updatedToken.RevokedAt.Should().NotBeNull();
        updatedToken.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task RevokeAllUserTokens_ShouldRevokeOnlyUserTokens()
    {
        // Arrange
        var userId = "test-user";
        var otherUserId = "other-user";
        
        var userTokens = new[]
        {
            CreateTestRefreshToken(userId, "token1"),
            CreateTestRefreshToken(userId, "token2")
        };
        
        var otherUserToken = CreateTestRefreshToken(otherUserId, "token3");

        await _refreshTokenRepository.AddRangeAsync(userTokens.Concat(new[] { otherUserToken }));
        await _unitOfWork.SaveChangesAsync();

        // Act
        var tokensToRevoke = await _refreshTokenRepository.FindAsync(rt => rt.UserId == userId);
        foreach (var token in tokensToRevoke)
        {
            token.Revoke();
        }
        _refreshTokenRepository.UpdateRange(tokensToRevoke);
        await _unitOfWork.SaveChangesAsync();

        // Assert
        var allTokens = await _refreshTokenRepository.GetAllAsync();
        var userRevokedTokens = allTokens.Where(rt => rt.UserId == userId);
        var otherUserTokens = allTokens.Where(rt => rt.UserId == otherUserId);

        userRevokedTokens.Should().OnlyContain(rt => rt.IsRevoked);
        otherUserTokens.Should().OnlyContain(rt => !rt.IsRevoked);
    }

    [Fact]
    public async Task CleanupExpiredTokens_ShouldRemoveOnlyExpiredTokens()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var tokens = new[]
        {
            CreateTestRefreshToken("user1", "expired1", now.AddDays(-2)),
            CreateTestRefreshToken("user1", "expired2", now.AddDays(-1)),
            CreateTestRefreshToken("user2", "valid1", now.AddDays(1)),
            CreateTestRefreshToken("user2", "valid2", now.AddDays(7))
        };

        await _refreshTokenRepository.AddRangeAsync(tokens);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var expiredTokens = await _refreshTokenRepository.FindAsync(
            rt => rt.ExpiresAt < now);
        _refreshTokenRepository.RemoveRange(expiredTokens);
        await _unitOfWork.SaveChangesAsync();

        // Assert
        var remainingTokens = await _refreshTokenRepository.GetAllAsync();
        remainingTokens.Should().HaveCount(2);
        remainingTokens.Should().OnlyContain(rt => rt.ExpiresAt > now);
    }

    #endregion

    #region 安全性測試

    [Fact]
    public async Task RefreshToken_ShouldBeUniquePerUser()
    {
        // Arrange
        var userId = "test-user";
        var token1 = CreateTestRefreshToken(userId, "same-token");
        var token2 = CreateTestRefreshToken(userId, "same-token");

        await _refreshTokenRepository.AddAsync(token1);
        await _unitOfWork.SaveChangesAsync();

        // Act & Assert
        await _refreshTokenRepository.AddAsync(token2);
        var act = async () => await _unitOfWork.SaveChangesAsync();
        
        // 應該拋出異常，因為 token 值重複
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task GetValidTokensForUser_ShouldOnlyReturnValidTokens()
    {
        // Arrange
        var userId = "test-user";
        var now = DateTime.UtcNow;
        
        var tokens = new[]
        {
            CreateTestRefreshToken(userId, "valid", now.AddDays(7)),
            CreateTestRefreshToken(userId, "expired", now.AddDays(-1)),
            CreateTestRefreshToken(userId, "revoked", now.AddDays(7))
        };
        
        tokens[2].Revoke(); // 撤銷第三個 token

        await _refreshTokenRepository.AddRangeAsync(tokens);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var validTokens = await _refreshTokenRepository.FindAsync(
            rt => rt.UserId == userId && 
                  !rt.IsRevoked && 
                  rt.ExpiresAt > now);

        // Assert
        validTokens.Should().HaveCount(1);
        validTokens.First().Token.Should().Be("valid");
    }

    #endregion

    #region 裝置管理測試

    [Fact]
    public async Task RefreshToken_WithDeviceId_ShouldTrackDevice()
    {
        // Arrange
        var deviceId = "device-123";
        var refreshToken = CreateTestRefreshToken(deviceId: deviceId);

        // Act
        await _refreshTokenRepository.AddAsync(refreshToken);
        await _unitOfWork.SaveChangesAsync();

        // Assert
        var savedToken = await _refreshTokenRepository.GetByIdAsync(refreshToken.Id);
        savedToken!.DeviceId.Should().Be(deviceId);
    }

    [Fact]
    public async Task RevokeTokensForDevice_ShouldOnlyRevokeDeviceTokens()
    {
        // Arrange
        var userId = "test-user";
        var deviceId = "device-123";
        
        var tokens = new[]
        {
            CreateTestRefreshToken(userId, "token1", deviceId: deviceId),
            CreateTestRefreshToken(userId, "token2", deviceId: deviceId),
            CreateTestRefreshToken(userId, "token3", deviceId: "other-device"),
            CreateTestRefreshToken(userId, "token4") // 無 deviceId
        };

        await _refreshTokenRepository.AddRangeAsync(tokens);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var deviceTokens = await _refreshTokenRepository.FindAsync(
            rt => rt.UserId == userId && rt.DeviceId == deviceId);
        
        foreach (var token in deviceTokens)
        {
            token.Revoke();
        }
        
        _refreshTokenRepository.UpdateRange(deviceTokens);
        await _unitOfWork.SaveChangesAsync();

        // Assert
        var allTokens = await _refreshTokenRepository.FindAsync(rt => rt.UserId == userId);
        var revokedTokens = allTokens.Where(rt => rt.IsRevoked);
        var activeTokens = allTokens.Where(rt => !rt.IsRevoked);

        revokedTokens.Should().HaveCount(2);
        revokedTokens.Should().OnlyContain(rt => rt.DeviceId == deviceId);
        
        activeTokens.Should().HaveCount(2);
        activeTokens.Should().NotContain(rt => rt.DeviceId == deviceId);
    }

    #endregion

    #region 效能測試

    [Fact]
    public async Task BulkTokenOperations_ShouldBeEfficient()
    {
        // Arrange
        var userId = "bulk-test-user";
        var tokenCount = 100;
        var tokens = Enumerable.Range(1, tokenCount)
            .Select(i => CreateTestRefreshToken(userId, $"bulk-token-{i}"))
            .ToList();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        await _refreshTokenRepository.AddRangeAsync(tokens);
        await _unitOfWork.SaveChangesAsync();

        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000, "Bulk insert should be fast");
        
        var savedTokens = await _refreshTokenRepository.FindAsync(rt => rt.UserId == userId);
        savedTokens.Should().HaveCount(tokenCount);
    }

    [Fact]
    public async Task TokenQuery_WithComplexFilter_ShouldBeEfficient()
    {
        // Arrange
        var users = Enumerable.Range(1, 10).Select(i => $"user-{i}").ToList();
        var tokens = new List<RefreshToken>();
        
        foreach (var userId in users)
        {
            for (int i = 0; i < 10; i++)
            {
                tokens.Add(CreateTestRefreshToken(userId, $"{userId}-token-{i}"));
            }
        }

        await _refreshTokenRepository.AddRangeAsync(tokens);
        await _unitOfWork.SaveChangesAsync();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var validTokens = await _refreshTokenRepository.FindAsync(
            rt => !rt.IsRevoked && 
                  rt.ExpiresAt > DateTime.UtcNow &&
                  rt.UserId.StartsWith("user-"));

        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000, "Complex query should be fast");
        validTokens.Should().HaveCount(100); // 所有 token 都應該是有效的
    }

    #endregion

    #region 交易處理測試

    [Fact]
    public async Task RefreshTokenTransaction_PartialFailure_ShouldRollback()
    {
        // Arrange
        var userId = "transaction-user";
        var initialCount = await _refreshTokenRepository.CountAsync();

        // Act & Assert
        var act = async () => await _unitOfWork.ExecuteTransactionAsync(async () =>
        {
            // 成功操作：新增 token
            var token1 = CreateTestRefreshToken(userId, "token1");
            await _refreshTokenRepository.AddAsync(token1);
            await _unitOfWork.SaveChangesAsync();

            // 失敗操作：重複 token
            var token2 = CreateTestRefreshToken(userId, "token1"); // 相同的 token
            await _refreshTokenRepository.AddAsync(token2);
            await _unitOfWork.SaveChangesAsync(); // 這裡應該失敗

            return "Should not reach here";
        });

        await act.Should().ThrowAsync<Exception>();

        // Assert - 驗證回滾
        var finalCount = await _refreshTokenRepository.CountAsync();
        finalCount.Should().Be(initialCount);
    }

    #endregion

    #region 私有輔助方法

    private RefreshToken CreateTestRefreshToken(
        string userId = "test-user",
        string? token = null,
        DateTime? expiresAt = null,
        string? deviceId = null)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = token ?? Guid.NewGuid().ToString(),
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(7),
            DeviceId = deviceId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    #endregion
}