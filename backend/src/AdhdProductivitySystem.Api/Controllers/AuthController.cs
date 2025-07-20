using AdhdProductivitySystem.Api.Models;
using AdhdProductivitySystem.Application.Common.Interfaces;
using AdhdProductivitySystem.Domain.Entities;
using AdhdProductivitySystem.Domain.Enums;
using AdhdProductivitySystem.Infrastructure.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace AdhdProductivitySystem.Api.Controllers;

/// <summary>
/// Controller for authentication operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly JwtService _jwtService;
    private readonly PasswordService _passwordService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IApplicationDbContext context,
        JwtService jwtService,
        PasswordService passwordService,
        ILogger<AuthController> logger)
    {
        _context = context;
        _jwtService = jwtService;
        _passwordService = passwordService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    /// <param name="request">Registration request</param>
    /// <returns>Authentication response</returns>
    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var validationErrors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                
                return BadRequest(ApiResponse.CreateError(
                    "ValidationError",
                    "請求資料格式不正確",
                    details: new { ValidationErrors = validationErrors }
                ));
            }

            // Check if user already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower());

            if (existingUser != null)
            {
                return BadRequest(ApiResponse.CreateError(
                    "UserAlreadyExists",
                    "此電子郵件已被註冊，請使用其他電子郵件或嘗試登入"
                ));
            }

            // Validate password strength
            if (!_passwordService.IsPasswordStrong(request.Password))
            {
                return BadRequest(ApiResponse.CreateError(
                    "WeakPassword",
                    "密碼強度不足：需至少 8 個字元，包含大小寫字母、數字和特殊字元"
                ));
            }

            // Hash password
            var (hash, salt) = _passwordService.HashPassword(request.Password);

            // Create user
            var user = new User
            {
                Email = request.Email.ToLower(),
                Name = request.Name,
                PasswordHash = hash,
                PasswordSalt = salt,
                AdhdType = request.AdhdType,
                TimeZone = request.TimeZone ?? "UTC"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Generate tokens
            var accessToken = _jwtService.GenerateToken(user).Token;
            var refreshTokenResult = _jwtService.GenerateRefreshToken();
            
            // 儲存 refresh token 到資料庫
            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshTokenResult.Token,
                ExpiresAt = refreshTokenResult.ExpiresAt,
                DeviceId = Request.Headers["User-Agent"].ToString()
            };
            
            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            var response = new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshTokenResult.Token,
                User = new UserInfo
                {
                    Id = user.Id,
                    Email = user.Email,
                    Name = user.Name,
                    AdhdType = user.AdhdType,
                    TimeZone = user.TimeZone,
                    PreferredTheme = user.PreferredTheme,
                    IsOnboardingCompleted = user.IsOnboardingCompleted
                }
            };

            _logger.LogInformation("User registered successfully: {Email}", user.Email);
            return CreatedAtAction(nameof(GetCurrentUser), ApiResponse<AuthResponse>.CreateSuccess(response));
        }
        catch (Exception ex)
        {
            var errorId = Guid.NewGuid().ToString("N")[..8];
            _logger.LogError(ex, "使用者註冊失敗 - ErrorId: {ErrorId}, Email: {Email}", errorId, request.Email);
            return StatusCode(500, ApiResponse.CreateError(
                "RegistrationError",
                "註冊過程中發生錯誤，請稍後再試",
                errorId
            ));
        }
    }

    /// <summary>
    /// Login user
    /// </summary>
    /// <param name="request">Login request</param>
    /// <returns>Authentication response</returns>
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var validationErrors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                
                return BadRequest(ApiResponse.CreateError(
                    "ValidationError",
                    "請求資料格式不正確",
                    details: new { ValidationErrors = validationErrors }
                ));
            }

            // Find user
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower());

            if (user == null)
            {
                return Unauthorized(ApiResponse.CreateError(
                    "InvalidCredentials",
                    "電子郵件或密碼錯誤"
                ));
            }

            // Verify password
            if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt))
            {
                return Unauthorized(ApiResponse.CreateError(
                    "InvalidCredentials",
                    "電子郵件或密碼錯誤"
                ));
            }

            // Update last active time
            user.LastActiveAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Generate tokens
            var accessToken = _jwtService.GenerateToken(user).Token;
            var refreshTokenResult = _jwtService.GenerateRefreshToken();
            
            // 儲存 refresh token 到資料庫
            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshTokenResult.Token,
                ExpiresAt = refreshTokenResult.ExpiresAt,
                DeviceId = Request.Headers["User-Agent"].ToString()
            };
            
            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            var response = new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshTokenResult.Token,
                User = new UserInfo
                {
                    Id = user.Id,
                    Email = user.Email,
                    Name = user.Name,
                    AdhdType = user.AdhdType,
                    TimeZone = user.TimeZone,
                    PreferredTheme = user.PreferredTheme,
                    IsOnboardingCompleted = user.IsOnboardingCompleted
                }
            };

            _logger.LogInformation("User logged in successfully: {Email}", user.Email);
            return Ok(ApiResponse<AuthResponse>.CreateSuccess(response));
        }
        catch (Exception ex)
        {
            var errorId = Guid.NewGuid().ToString("N")[..8];
            _logger.LogError(ex, "使用者登入失敗 - ErrorId: {ErrorId}, Email: {Email}", errorId, request.Email);
            return StatusCode(500, ApiResponse.CreateError(
                "LoginError",
                "登入過程中發生錯誤，請稍後再試",
                errorId
            ));
        }
    }

    /// <summary>
    /// Get current user information
    /// </summary>
    /// <returns>Current user information</returns>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<UserInfo>>> GetCurrentUser()
    {
        try
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(ApiResponse.CreateError(
                    "InvalidToken",
                    "無效的身分驗證令牌"
                ));
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return Unauthorized(ApiResponse.CreateError(
                    "UserNotFound",
                    "找不到使用者資料"
                ));
            }

            var userInfo = new UserInfo
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                AdhdType = user.AdhdType,
                TimeZone = user.TimeZone,
                PreferredTheme = user.PreferredTheme,
                IsOnboardingCompleted = user.IsOnboardingCompleted,
                ProfilePictureUrl = user.ProfilePictureUrl
            };

            return Ok(ApiResponse<UserInfo>.CreateSuccess(userInfo));
        }
        catch (Exception ex)
        {
            var errorId = Guid.NewGuid().ToString("N")[..8];
            _logger.LogError(ex, "獲取使用者資訊失敗 - ErrorId: {ErrorId}", errorId);
            return StatusCode(500, ApiResponse.CreateError(
                "UserInfoError",
                "獲取使用者資訊時發生錯誤，請稍後再試",
                errorId
            ));
        }
    }

    /// <summary>
    /// Refresh access token
    /// </summary>
    /// <param name="request">Refresh token request</param>
    /// <returns>New access token</returns>
    [HttpPost("refresh")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(ApiResponse<RefreshResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ApiResponse<RefreshResponse>>> RefreshToken([FromBody] RefreshRequest request)
    {
        try
        {
            // 從資料庫查詢 refresh token
            var existingToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

            if (existingToken == null)
            {
                return Unauthorized(ApiResponse.CreateError(
                    "InvalidRefreshToken",
                    "無效的重新整理令牌"
                ));
            }

            // 檢查 token 是否有效
            if (!existingToken.IsValid)
            {
                return Unauthorized(ApiResponse.CreateError(
                    "ExpiredRefreshToken",
                    "重新整理令牌已過期或被撤銷"
                ));
            }

            var user = existingToken.User;
            if (user == null)
            {
                return Unauthorized(ApiResponse.CreateError(
                    "UserNotFound",
                    "找不到使用者資料"
                ));
            }

            // 撤銷舊的 refresh token
            existingToken.Revoke();

            // 產生新的 tokens
            var newAccessToken = _jwtService.GenerateToken(user).Token;
            var newRefreshTokenResult = _jwtService.GenerateRefreshToken();
            
            // 儲存新的 refresh token
            var newRefreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = newRefreshTokenResult.Token,
                ExpiresAt = newRefreshTokenResult.ExpiresAt,
                DeviceId = Request.Headers["User-Agent"].ToString()
            };
            
            _context.RefreshTokens.Add(newRefreshToken);
            await _context.SaveChangesAsync();

            var response = new RefreshResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshTokenResult.Token
            };

            return Ok(ApiResponse<RefreshResponse>.CreateSuccess(response));
        }
        catch (Exception ex)
        {
            var errorId = Guid.NewGuid().ToString("N")[..8];
            _logger.LogError(ex, "Token刷新失敗 - ErrorId: {ErrorId}", errorId);
            return StatusCode(500, ApiResponse.CreateError(
                "TokenRefreshError",
                "Token刷新時發生錯誤，請重新登入",
                errorId
            ));
        }
    }

    /// <summary>
    /// Logout user
    /// </summary>
    /// <returns>Success response</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> Logout()
    {
        try
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Ok(ApiResponse.CreateSuccess("已成功登出"));
            }

            // 撤銷該使用者所有有效的 refresh tokens
            var activeTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                .ToListAsync();

            foreach (var token in activeTokens)
            {
                token.Revoke();
            }

            await _context.SaveChangesAsync();

            return Ok(ApiResponse.CreateSuccess("已成功登出"));
        }
        catch (Exception ex)
        {
            var errorId = Guid.NewGuid().ToString("N")[..8];
            _logger.LogError(ex, "登出過程發生錯誤 - ErrorId: {ErrorId}", errorId);
            // 即使出錯也返回成功，避免洩漏錯誤訊息，但提供錯誤ID供追蹤
            return Ok(ApiResponse.CreateSuccess("已成功登出", new ApiMeta { Additional = new Dictionary<string, object> { { "errorId", errorId } } }));
        }
    }
}