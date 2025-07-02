using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xunit;
using AdhdProductivitySystem.Infrastructure.Data;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace AdhdProductivitySystem.Tests.Integration;

public class AuthenticationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly PostgreSqlContainer _postgres;
    private readonly RedisContainer _redis;

    public AuthenticationIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _postgres = new PostgreSqlBuilder()
            .WithDatabase("adhd_test")
            .WithUsername("test_user")
            .WithPassword("test_pass")
            .Build();

        _redis = new RedisBuilder()
            .Build();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add test database
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseNpgsql(_postgres.GetConnectionString());
                });

                // Override Redis configuration
                services.Configure<RedisOptions>(options =>
                {
                    options.ConnectionString = _redis.GetConnectionString();
                });
            });
        });

        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _redis.StartAsync();

        // Run migrations
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _redis.DisposeAsync();
        _client.Dispose();
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var registerRequest = new
        {
            Email = "test@example.com",
            Password = "TestPassword123!",
            ConfirmPassword = "TestPassword123!",
            FirstName = "Test",
            LastName = "User",
            AdhdType = "Combined",
            Theme = "Light"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<dynamic>(content);
        
        Assert.NotNull(result);
        // Additional assertions can be added based on the expected response structure
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var registerRequest = new
        {
            Email = "invalid-email",
            Password = "TestPassword123!",
            ConfirmPassword = "TestPassword123!",
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokenAndUserInfo()
    {
        // Arrange
        // First register a user
        var registerRequest = new
        {
            Email = "login-test@example.com",
            Password = "TestPassword123!",
            ConfirmPassword = "TestPassword123!",
            FirstName = "Login",
            LastName = "Test",
            AdhdType = "Combined",
            Theme = "Light"
        };

        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new
        {
            Email = "login-test@example.com",
            Password = "TestPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(content);
        
        Assert.True(document.RootElement.TryGetProperty("token", out _));
        Assert.True(document.RootElement.TryGetProperty("user", out _));
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new
        {
            Email = "nonexistent@example.com",
            Password = "WrongPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/tasks");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidToken_ReturnsSuccess()
    {
        // Arrange
        var token = await GetValidAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/tasks");

        // Assert
        Assert.True(response.IsSuccessStatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("email@")]
    [InlineData("@domain.com")]
    public async Task Register_WithInvalidEmailFormats_ReturnsBadRequest(string email)
    {
        // Arrange
        var registerRequest = new
        {
            Email = email,
            Password = "TestPassword123!",
            ConfirmPassword = "TestPassword123!",
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("123")]        // Too short
    [InlineData("password")]   // No uppercase or numbers
    [InlineData("PASSWORD")]   // No lowercase or numbers
    [InlineData("Password")]   // No numbers
    [InlineData("Password123")] // No special characters (depending on requirements)
    public async Task Register_WithWeakPasswords_ReturnsBadRequest(string password)
    {
        // Arrange
        var registerRequest = new
        {
            Email = "test@example.com",
            Password = password,
            ConfirmPassword = password,
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<string> GetValidAuthTokenAsync()
    {
        // Register and login to get a valid token
        var registerRequest = new
        {
            Email = "token-test@example.com",
            Password = "TestPassword123!",
            ConfirmPassword = "TestPassword123!",
            FirstName = "Token",
            LastName = "Test",
            AdhdType = "Combined",
            Theme = "Light"
        };

        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new
        {
            Email = "token-test@example.com",
            Password = "TestPassword123!"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var content = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(content);
        
        return document.RootElement.GetProperty("token").GetString()!;
    }
}

public class RedisOptions
{
    public string ConnectionString { get; set; } = string.Empty;
}