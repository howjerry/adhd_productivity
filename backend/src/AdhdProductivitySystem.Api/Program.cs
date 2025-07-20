using AdhdProductivitySystem.Api.Hubs;
using AdhdProductivitySystem.Api.Middleware;
using AdhdProductivitySystem.Api.Services;
using AdhdProductivitySystem.Application.Common.Interfaces;
using AdhdProductivitySystem.Application.Features.Tasks.Commands.CreateTask;
using AdhdProductivitySystem.Application.Mappings;
using AdhdProductivitySystem.Infrastructure;
using AdhdProductivitySystem.Infrastructure.Authentication;
using AdhdProductivitySystem.Infrastructure.Data;
using AdhdProductivitySystem.Infrastructure.Security;
using AdhdProductivitySystem.Infrastructure.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Note: Prometheus metrics collection has been disabled for now

// Security validation - skip in development and testing for easier setup
if (builder.Environment.IsProduction())
{
    // Only validate security in production environment
    builder.Services.AddSecurityValidation(builder.Configuration);
}
else
{
    // Development/Testing: Log warning about skipped security validation
    var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger("SecurityValidation");
    logger.LogWarning("Security validation skipped for {Environment} environment", builder.Environment.EnvironmentName);
}

// Add security headers configuration
builder.Services.AddSecurityHeaders(builder.Configuration);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Configure Entity Framework - prioritize environment variables in Docker
var postgresHost = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost";
var postgresDb = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "adhd_productivity";
var postgresUser = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "adhd_user";
var postgresPassword = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "adhd_secure_pass_2024";
var postgresPort = int.TryParse(Environment.GetEnvironmentVariable("POSTGRES_PORT"), out var port) ? port : 5432;

var baseConnectionString = $"Host={postgresHost};Database={postgresDb};Username={postgresUser};Password={postgresPassword};Port={postgresPort}";

// 使用 PostgreSQL 配置優化連線字串
var connectionString = AdhdProductivitySystem.Infrastructure.Data.PostgreSQLConfiguration.ConfigureConnectionString(baseConnectionString);

// Set connection string in configuration for Infrastructure layer
builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;

// Fallback to configuration if env vars not available (for local development)
if (string.IsNullOrEmpty(postgresHost) || postgresHost == "localhost")
{
    var configConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrEmpty(configConnectionString))
    {
        connectionString = configConnectionString;
    }
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        // 設定 PostgreSQL 特定選項
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
        
        // 啟用敏感資料記錄（僅開發環境）
        if (builder.Environment.IsDevelopment())
        {
            // npgsqlOptions.EnableSensitiveDataLogging(); // 僅用於開發階段除錯
        }
        
        // 設定命令超時
        npgsqlOptions.CommandTimeout(60);
    });
    
    // 設定 EF Core 選項
    if (builder.Environment.IsDevelopment())
    {
        // options.EnableSensitiveDataLogging(); // 僅用於開發階段除錯
        options.EnableDetailedErrors();
    }
    
    // Query splitting configured at query level when needed
});

builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

// Configure Redis caching
var redisHost = Environment.GetEnvironmentVariable("REDIS_HOST") ?? "localhost";
var redisPort = Environment.GetEnvironmentVariable("REDIS_PORT") ?? "6379";
var redisPassword = Environment.GetEnvironmentVariable("REDIS_PASSWORD");
var redisConnection = $"{redisHost}:{redisPort}";
if (!string.IsNullOrEmpty(redisPassword))
{
    redisConnection += $",password={redisPassword}";
}

// Always add memory cache first as fallback
builder.Services.AddMemoryCache();

// Try to connect to Redis, fall back to in-memory cache if failed
try
{
    if (redisHost != "localhost" || !string.IsNullOrEmpty(redisPassword))
    {
        // Use Redis for production or when specifically configured
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection;
            options.InstanceName = "ADHDProductivitySystem";
        });
        
        // Add Redis connection multiplexer for advanced operations
        builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(sp =>
        {
            return StackExchange.Redis.ConnectionMultiplexer.Connect(redisConnection);
        });
    }
    else
    {
        // Use in-memory cache for development
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(sp => null!);
    }
}
catch (Exception ex)
{
    // If Redis configuration fails, fall back to in-memory cache
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(sp => null!);
    
    var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger("Redis");
    logger.LogWarning(ex, "Failed to configure Redis, using in-memory cache");
}

// Configure Authentication with environment variable fallback
var jwtSettings = builder.Configuration.GetSection("JWT");
var secretKey = Environment.GetEnvironmentVariable("JWT__SecretKey") ?? 
               Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ??
               jwtSettings["SecretKey"] ?? 
               (builder.Environment.IsDevelopment() 
                   ? "Development_ADHD_JWT_Secret_Key_For_Local_Testing_2024_MinimumLength32" 
                   : throw new ArgumentNullException("JWT SecretKey not configured"));
var key = Encoding.ASCII.GetBytes(secretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment(); // Require HTTPS in production
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        RequireExpirationTime = true,
        RequireSignedTokens = true,
        RequireAudience = true,
        ClockSkew = TimeSpan.FromSeconds(30), // 縮短時鐘偏差容忍度
        LifetimeValidator = (notBefore, expires, token, parameters) =>
        {
            var now = DateTime.UtcNow;
            if (notBefore.HasValue && notBefore.Value > now.AddMinutes(5))
                return false;
            if (expires.HasValue && expires.Value < now)
                return false;
            return true;
        }
    };

    // Configure JWT for SignalR
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            
            if (!string.IsNullOrEmpty(accessToken) && 
                (path.StartsWithSegments("/hubs/timer") || path.StartsWithSegments("/hubs/notification")))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// Configure Authorization
builder.Services.AddAuthorization();

// Configure Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    // 設定認證端點的速率限制 - 更嚴格
    options.AddPolicy<string>("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: "auth",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 5, // 每分鐘最多 5 次請求
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    // 設定一般 API 端點的速率限制 - 較寬鬆
    options.AddPolicy<string>("api", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: "api",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 60, // 每分鐘最多 60 次請求
                QueueLimit = 10,
                AutoReplenishment = true
            }));

    // 設定全域的速率限制策略
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        // 根據 IP 地址進行限制
        var userIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: userIp,
            factory: partition => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 100, // 每個 IP 每分鐘最多 100 次請求
                QueueLimit = 20,
                AutoReplenishment = true
            });
    });

    // 自訂速率限制回應
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";

        // 添加速率限制相關的標頭
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = retryAfter.TotalSeconds.ToString();
        }

        // 添加速率限制資訊標頭
        context.HttpContext.Response.Headers["X-RateLimit-Limit"] = "60";
        context.HttpContext.Response.Headers["X-RateLimit-Remaining"] = "0";
        
        // 計算重置時間
        var currentTime = DateTimeOffset.UtcNow;
        var resetTime = currentTime.AddMinutes(1).ToUnixTimeSeconds();
        context.HttpContext.Response.Headers["X-RateLimit-Reset"] = resetTime.ToString();

        var response = new
        {
            error = "TooManyRequests",
            message = "請求次數過多，請稍後再試。",
            retryAfterSeconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry) 
                ? retry.TotalSeconds 
                : 60
        };

        await context.HttpContext.Response.WriteAsync(
            System.Text.Json.JsonSerializer.Serialize(response),
            cancellationToken);
    };
});

// Add MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateTaskCommand).Assembly));

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Add FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(CreateTaskCommand).Assembly);

// Add Infrastructure layer services
builder.Services.AddInfrastructure(builder.Configuration);

// Add application services
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddHttpContextAccessor();

// Add SignalR
builder.Services.AddSignalR();

// Add CORS with enhanced security
builder.Services.AddCors(options =>
{
    // 開發環境的CORS設定 - 僅在開發環境使用
    options.AddPolicy("Development", policy =>
    {
        var allowedOrigins = new[] { 
            "http://localhost:3000", 
            "https://localhost:3000",
            "http://127.0.0.1:3000",
            "https://127.0.0.1:3000"
        };
        
        policy.WithOrigins(allowedOrigins)
              .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS", "PATCH") // 明確指定允許的方法
              .WithHeaders("Content-Type", "Authorization", "X-Requested-With", "Accept", "Origin") // 明確指定允許的標頭
              .AllowCredentials()
              .SetPreflightMaxAge(TimeSpan.FromHours(1)); // 設定預檢請求快取時間
    });
    
    // 生產環境的CORS設定 - 更嚴格的安全配置
    options.AddPolicy("Production", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() 
                           ?? builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                           ?? new[] { "https://your-production-domain.com" };
        
        // 驗證所有允許的來源都是HTTPS（生產環境）
        var validOrigins = allowedOrigins.Where(origin => 
            Uri.TryCreate(origin, UriKind.Absolute, out var uri) && 
            (uri.Scheme == "https" || uri.IsLoopback)).ToArray();
            
        if (validOrigins.Length == 0)
        {
            throw new InvalidOperationException("No valid HTTPS origins configured for production CORS policy");
        }
        
        policy.WithOrigins(validOrigins)
              .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS", "PATCH")
              .WithHeaders("Content-Type", "Authorization", "X-Requested-With", "Accept", "Origin")
              .AllowCredentials()
              .SetPreflightMaxAge(TimeSpan.FromHours(1))
; // 不允許動態來源驗證
    });
    
    // SignalR專用的CORS設定
    options.AddPolicy("SignalR", policy =>
    {
        var environment = builder.Environment;
        if (environment.IsDevelopment())
        {
            policy.WithOrigins("http://localhost:3000", "https://localhost:3000")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
        else
        {
            var signalROrigins = builder.Configuration.GetSection("SignalR:AllowedOrigins").Get<string[]>()
                               ?? builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                               ?? new[] { "https://your-production-domain.com" };
            
            policy.WithOrigins(signalROrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
    });
});

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ADHD Productivity System API",
        Version = "v1",
        Description = "A comprehensive productivity system designed specifically for individuals with ADHD",
        Contact = new OpenApiContact
        {
            Name = "ADHD Productivity System",
            Email = "support@adhdproductivity.com"
        }
    });

    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });

    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>();

// Note: Metrics service disabled for now

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ADHD Productivity System API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at the app's root
    });
    app.UseCors("Development");
}
else
{
    app.UseHsts();
    app.UseCors("Production");
}

// Use global exception handling middleware
app.UseGlobalExceptionHandling();

app.UseHttpsRedirection();

// Enable structured logging (early in pipeline)
app.UseStructuredLogging();

// Enable Prometheus metrics collection
// Note: Prometheus metrics disabled

// Enable input validation middleware
app.UseInputValidation();

// Enable performance monitoring
app.UsePerformanceMonitoring();

// Enable API request logging
app.UseApiLogging();

// Enable security monitoring
app.UseSecurityMonitoring(new SecurityMonitoringOptions
{
    MaxRequestsPerMinute = 100,
    MaxFailedAuthAttemptsPerHour = 20,
    Max404ErrorsPerHour = 50
});

// Enable security headers middleware (replaces manual header setting)
app.UseSecurityHeaders();

app.UseAuthentication();
app.UseAuthorization();

// 添加速率限制中間件
app.UseRateLimiter();

app.MapControllers();

// Map SignalR hubs
app.MapHub<TimerHub>("/hubs/timer");
app.MapHub<NotificationHub>("/hubs/notification");

// Map health checks
app.MapHealthChecks("/health");

// Note: Metrics endpoint disabled for now

// Create database if it doesn't exist (development only)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();
}

// Note: Global exception handling is now handled by GlobalExceptionMiddleware

try
{
    Log.Information("Starting ADHD Productivity System API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make the implicit Program class public so test projects can access it
public partial class Program { }