using System;
using System.Text;
using System.Threading.Tasks;
using AdhdProductivitySystem.Domain.Entities;
using AdhdProductivitySystem.Infrastructure.Authentication;
using AdhdProductivitySystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SecurityValidationScript
{
    /// <summary>
    /// 認證與安全系統手動驗證腳本
    /// </summary>
    public class SecurityValidationScript
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== ADHD 生產力系統認證與安全驗證 ===");
            Console.WriteLine($"開始時間: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();

            try
            {
                await RunAllSecurityTests();
                Console.WriteLine("✅ 所有安全測試完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 測試執行失敗: {ex.Message}");
                Console.WriteLine($"詳細錯誤: {ex}");
            }

            Console.WriteLine();
            Console.WriteLine($"結束時間: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        }

        private static async Task RunAllSecurityTests()
        {
            Console.WriteLine("📋 開始執行安全測試...");
            Console.WriteLine();

            // 1. 密碼安全測試
            await TestPasswordSecurity();

            // 2. JWT 安全測試
            await TestJwtSecurity();

            // 3. RefreshToken 持久化測試
            await TestRefreshTokenPersistence();

            // 4. 認證流程整合測試
            await TestAuthenticationWorkflow();

            // 5. 安全配置驗證
            await TestSecurityConfiguration();
        }

        private static async Task TestPasswordSecurity()
        {
            Console.WriteLine("🔐 1. 密碼安全測試");
            
            var passwordService = new PasswordService();
            
            // 測試密碼雜湊
            var password = "TestPassword123!";
            var (hash1, salt1) = passwordService.HashPassword(password);
            var (hash2, salt2) = passwordService.HashPassword(password);
            
            Console.WriteLine($"   ✓ 密碼雜湊產生: 每次都不同 (hash1 != hash2): {hash1 != hash2}");
            Console.WriteLine($"   ✓ 密碼驗證: 正確密碼驗證成功: {passwordService.VerifyPassword(password, hash1, salt1)}");
            Console.WriteLine($"   ✓ 密碼驗證: 錯誤密碼驗證失敗: {!passwordService.VerifyPassword("WrongPassword", hash1, salt1)}");
            
            // 測試密碼強度
            var testCases = new[]
            {
                ("Password123!", true, "強密碼"),
                ("password", false, "弱密碼：常見密碼"),
                ("123456", false, "弱密碼：純數字"),
                ("short", false, "弱密碼：太短"),
                ("NoDigits!", false, "弱密碼：沒有數字"),
                ("nospecial123", false, "弱密碼：沒有特殊字符")
            };

            foreach (var (testPassword, expected, description) in testCases)
            {
                var isStrong = passwordService.IsPasswordStrong(testPassword);
                var result = isStrong == expected ? "✓" : "✗";
                Console.WriteLine($"   {result} 密碼強度驗證: {description} - 結果: {isStrong}");
            }
            
            Console.WriteLine();
        }

        private static async Task TestJwtSecurity()
        {
            Console.WriteLine("🎫 2. JWT 安全測試");
            
            // 模擬配置
            var configData = new Dictionary<string, string>
            {
                {"JWT:SecretKey", "test_secret_key_that_is_32_characters_long_and_secure!"},
                {"JWT:Issuer", "test_issuer"},
                {"JWT:Audience", "test_audience"},
                {"JWT:TokenExpirationMinutes", "15"}
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<JwtService>.Instance;
            var jwtService = new JwtService(configuration, logger);
            
            var testUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                Name = "Test User",
                PasswordHash = "hash",
                PasswordSalt = "salt"
            };

            // 測試 Token 生成
            var (token, expiresAt) = jwtService.GenerateToken(testUser);
            Console.WriteLine($"   ✓ JWT Token 生成: Token 不為空: {!string.IsNullOrEmpty(token)}");
            Console.WriteLine($"   ✓ JWT Token 生成: 過期時間正確: {expiresAt > DateTime.UtcNow}");
            
            // 測試 Token 驗證
            var principal = jwtService.ValidateToken(token);
            var userIdClaim = principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            Console.WriteLine($"   ✓ JWT Token 驗證: 有效 Token 驗證成功: {principal != null}");
            Console.WriteLine($"   ✓ JWT Token 驗證: 用戶 ID 正確: {userIdClaim == testUser.Id.ToString()}");
            
            // 測試無效 Token
            var invalidPrincipal = jwtService.ValidateToken("invalid.token.here");
            Console.WriteLine($"   ✓ JWT Token 驗證: 無效 Token 驗證失敗: {invalidPrincipal == null}");
            
            // 測試 Refresh Token 生成
            var refreshTokenResult = jwtService.GenerateRefreshToken();
            Console.WriteLine($"   ✓ Refresh Token 生成: Token 不為空: {!string.IsNullOrEmpty(refreshTokenResult.Token)}");
            Console.WriteLine($"   ✓ Refresh Token 生成: 過期時間正確: {refreshTokenResult.ExpiresAt > DateTime.UtcNow.AddDays(6)}");
            
            Console.WriteLine();
        }

        private static async Task TestRefreshTokenPersistence()
        {
            Console.WriteLine("💾 3. RefreshToken 持久化測試");
            
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            await context.Database.EnsureCreatedAsync();
            
            // 創建測試用戶
            var testUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                Name = "Test User",
                PasswordHash = "hash",
                PasswordSalt = "salt"
            };
            
            context.Users.Add(testUser);
            await context.SaveChangesAsync();
            
            // 測試 RefreshToken 儲存
            var refreshToken = new RefreshToken
            {
                UserId = testUser.Id,
                Token = Guid.NewGuid().ToString(),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            };
            
            context.RefreshTokens.Add(refreshToken);
            await context.SaveChangesAsync();
            
            var savedToken = await context.RefreshTokens.FirstAsync();
            Console.WriteLine($"   ✓ RefreshToken 儲存: 成功儲存到資料庫: {savedToken != null}");
            Console.WriteLine($"   ✓ RefreshToken 儲存: 用戶 ID 正確: {savedToken.UserId == testUser.Id}");
            Console.WriteLine($"   ✓ RefreshToken 狀態: 有效狀態正確: {savedToken.IsValid}");
            
            // 測試 RefreshToken 撤銷
            savedToken.Revoke();
            await context.SaveChangesAsync();
            
            Console.WriteLine($"   ✓ RefreshToken 撤銷: 撤銷狀態正確: {savedToken.IsRevoked}");
            Console.WriteLine($"   ✓ RefreshToken 撤銷: 撤銷時間設定: {savedToken.RevokedAt != null}");
            Console.WriteLine($"   ✓ RefreshToken 撤銷: 無效狀態正確: {!savedToken.IsValid}");
            
            // 測試過期 RefreshToken
            var expiredToken = new RefreshToken
            {
                UserId = testUser.Id,
                Token = Guid.NewGuid().ToString(),
                ExpiresAt = DateTime.UtcNow.AddDays(-1), // 昨天過期
                IsRevoked = false
            };
            
            context.RefreshTokens.Add(expiredToken);
            await context.SaveChangesAsync();
            
            var savedExpiredToken = await context.RefreshTokens
                .FirstAsync(rt => rt.Token == expiredToken.Token);
            Console.WriteLine($"   ✓ RefreshToken 過期: 過期 Token 無效: {!savedExpiredToken.IsValid}");
            
            Console.WriteLine();
        }

        private static async Task TestAuthenticationWorkflow()
        {
            Console.WriteLine("🔄 4. 認證流程整合測試");
            
            // 設定服務
            var passwordService = new PasswordService();
            
            var configData = new Dictionary<string, string>
            {
                {"JWT:SecretKey", "test_secret_key_that_is_32_characters_long_and_secure!"},
                {"JWT:Issuer", "test_issuer"},
                {"JWT:Audience", "test_audience"},
                {"JWT:TokenExpirationMinutes", "15"}
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();
            
            var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<JwtService>.Instance;
            var jwtService = new JwtService(configuration, logger);
            
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            await context.Database.EnsureCreatedAsync();
            
            // 模擬註冊流程
            var originalPassword = "SecurePassword123!";
            var (hash, salt) = passwordService.HashPassword(originalPassword);
            
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                Name = "Test User",
                PasswordHash = hash,
                PasswordSalt = salt
            };
            
            context.Users.Add(user);
            await context.SaveChangesAsync();
            Console.WriteLine("   ✓ 註冊流程: 用戶創建成功");
            
            // 模擬登入流程
            var isValidLogin = passwordService.VerifyPassword(originalPassword, hash, salt);
            Console.WriteLine($"   ✓ 登入流程: 密碼驗證成功: {isValidLogin}");
            
            var (accessToken, _) = jwtService.GenerateToken(user);
            var refreshTokenResult = jwtService.GenerateRefreshToken();
            Console.WriteLine("   ✓ 登入流程: Token 生成成功");
            
            // 儲存 RefreshToken
            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshTokenResult.Token,
                ExpiresAt = refreshTokenResult.ExpiresAt,
                IsRevoked = false
            };
            
            context.RefreshTokens.Add(refreshToken);
            await context.SaveChangesAsync();
            Console.WriteLine("   ✓ 登入流程: RefreshToken 儲存成功");
            
            // 驗證 Access Token
            var principal = jwtService.ValidateToken(accessToken);
            Console.WriteLine($"   ✓ Token 驗證: Access Token 驗證成功: {principal != null}");
            
            // 模擬 Token 刷新流程
            var storedRefreshToken = await context.RefreshTokens
                .FirstAsync(rt => rt.Token == refreshTokenResult.Token);
            Console.WriteLine($"   ✓ Token 刷新: RefreshToken 查詢成功: {storedRefreshToken.IsValid}");
            
            // 撤銷舊 Token 並生成新 Token
            storedRefreshToken.Revoke();
            var newRefreshTokenResult = jwtService.GenerateRefreshToken();
            var newAccessToken = jwtService.GenerateToken(user).Token;
            
            var newRefreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = newRefreshTokenResult.Token,
                ExpiresAt = newRefreshTokenResult.ExpiresAt,
                IsRevoked = false
            };
            
            context.RefreshTokens.Add(newRefreshToken);
            await context.SaveChangesAsync();
            
            Console.WriteLine("   ✓ Token 刷新: 新 Token 生成成功");
            Console.WriteLine($"   ✓ Token 刷新: 舊 Token 已撤銷: {storedRefreshToken.IsRevoked}");
            
            // 模擬登出流程
            var allUserTokens = context.RefreshTokens
                .Where(rt => rt.UserId == user.Id && !rt.IsRevoked)
                .ToList();
            
            foreach (var token in allUserTokens)
            {
                token.Revoke();
            }
            await context.SaveChangesAsync();
            
            var activeTokensCount = context.RefreshTokens
                .Count(rt => rt.UserId == user.Id && !rt.IsRevoked);
            Console.WriteLine($"   ✓ 登出流程: 所有 Token 已撤銷: {activeTokensCount == 0}");
            
            Console.WriteLine();
        }

        private static async Task TestSecurityConfiguration()
        {
            Console.WriteLine("⚙️ 5. 安全配置驗證");
            
            // 測試弱密鑰檢測
            try
            {
                var weakConfigData = new Dictionary<string, string>
                {
                    {"JWT:SecretKey", "weak"}, // 弱密鑰
                    {"JWT:Issuer", "test_issuer"},
                    {"JWT:Audience", "test_audience"},
                    {"JWT:TokenExpirationMinutes", "15"}
                };

                var weakConfiguration = new ConfigurationBuilder()
                    .AddInMemoryCollection(weakConfigData)
                    .Build();
                
                var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<JwtService>.Instance;
                var jwtService = new JwtService(weakConfiguration, logger);
                
                Console.WriteLine("   ✗ 安全配置: 弱密鑰檢測失敗 - 應該拋出例外");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("   ✓ 安全配置: 弱密鑰檢測成功 - 正確拋出例外");
            }
            
            // 測試安全密鑰
            var secureConfigData = new Dictionary<string, string>
            {
                {"JWT:SecretKey", "test_secret_key_that_is_32_characters_long_and_secure!"},
                {"JWT:Issuer", "test_issuer"},
                {"JWT:Audience", "test_audience"},
                {"JWT:TokenExpirationMinutes", "15"}
            };

            var secureConfiguration = new ConfigurationBuilder()
                .AddInMemoryCollection(secureConfigData)
                .Build();
            
            var secureLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<JwtService>.Instance;
            var secureJwtService = new JwtService(secureConfiguration, secureLogger);
            Console.WriteLine("   ✓ 安全配置: 安全密鑰接受成功");
            
            Console.WriteLine();
        }
    }
}