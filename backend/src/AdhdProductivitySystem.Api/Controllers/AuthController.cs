using AdhdProductivitySystem.Application.Common.Interfaces;
using AdhdProductivitySystem.Domain.Entities;
using AdhdProductivitySystem.Domain.Enums;
using AdhdProductivitySystem.Infrastructure.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if user already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower());

            if (existingUser != null)
            {
                return BadRequest("User with this email already exists");
            }

            // Validate password strength
            if (!_passwordService.IsPasswordStrong(request.Password))
            {
                return BadRequest("Password must be at least 8 characters long and contain uppercase, lowercase, digit, and special character");
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
            var accessToken = _jwtService.GenerateToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            var response = new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
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
            return CreatedAtAction(nameof(GetCurrentUser), response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user");
            return StatusCode(500, "An error occurred while registering the user");
        }
    }

    /// <summary>
    /// Login user
    /// </summary>
    /// <param name="request">Login request</param>
    /// <returns>Authentication response</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Find user
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower());

            if (user == null)
            {
                return Unauthorized("Invalid email or password");
            }

            // Verify password
            if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt))
            {
                return Unauthorized("Invalid email or password");
            }

            // Update last active time
            user.LastActiveAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Generate tokens
            var accessToken = _jwtService.GenerateToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            var response = new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
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
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging in user");
            return StatusCode(500, "An error occurred while logging in");
        }
    }

    /// <summary>
    /// Get current user information
    /// </summary>
    /// <returns>Current user information</returns>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserInfo>> GetCurrentUser()
    {
        try
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return Unauthorized();
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

            return Ok(userInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return StatusCode(500, "An error occurred while getting user information");
        }
    }

    /// <summary>
    /// Refresh access token
    /// </summary>
    /// <param name="request">Refresh token request</param>
    /// <returns>New access token</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RefreshResponse>> RefreshToken([FromBody] RefreshRequest request)
    {
        try
        {
            // Validate the refresh token (in a real implementation, you'd store refresh tokens in the database)
            // For simplicity, we'll just validate the access token and generate a new one
            var principal = _jwtService.ValidateToken(request.AccessToken);
            if (principal == null)
            {
                return Unauthorized("Invalid token");
            }

            var userIdClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized("Invalid token");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return Unauthorized("User not found");
            }

            // Generate new tokens
            var newAccessToken = _jwtService.GenerateToken(user);
            var newRefreshToken = _jwtService.GenerateRefreshToken();

            var response = new RefreshResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return StatusCode(500, "An error occurred while refreshing the token");
        }
    }

    /// <summary>
    /// Logout user
    /// </summary>
    /// <returns>Success response</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        // In a real implementation, you'd invalidate the refresh token
        // For now, we'll just return success
        return Ok(new { message = "Logged out successfully" });
    }
}

// DTOs for authentication
public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public AdhdType AdhdType { get; set; }
    public string? TimeZone { get; set; }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RefreshRequest
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public UserInfo User { get; set; } = new();
}

public class RefreshResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}

public class UserInfo
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AdhdType AdhdType { get; set; }
    public string TimeZone { get; set; } = string.Empty;
    public Theme PreferredTheme { get; set; }
    public bool IsOnboardingCompleted { get; set; }
    public string? ProfilePictureUrl { get; set; }
}