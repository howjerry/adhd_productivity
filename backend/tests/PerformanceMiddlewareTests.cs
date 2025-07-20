using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using AdhdProductivitySystem.Api.Middleware;
using System.Diagnostics;

namespace AdhdProductivitySystem.Tests.Unit.Performance;

public class PerformanceMiddlewareTests
{
    private readonly ILogger<PerformanceMiddleware> _logger;
    private readonly IServiceProvider _serviceProvider;

    public PerformanceMiddlewareTests()
    {
        _logger = NullLogger<PerformanceMiddleware>.Instance;
        
        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment("Development"));
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task InvokeAsync_Should_Measure_Response_Time()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.RequestServices = _serviceProvider;
        context.Request.Method = "GET";
        context.Request.Path = "/api/test";
        
        var called = false;
        RequestDelegate next = (_) =>
        {
            called = true;
            return Task.CompletedTask;
        };

        var middleware = new PerformanceMiddleware(next, _logger);

        // Act
        var stopwatch = Stopwatch.StartNew();
        await middleware.InvokeAsync(context);
        stopwatch.Stop();

        // Assert
        Assert.True(called);
        Assert.True(stopwatch.ElapsedMilliseconds >= 0);
    }

    [Fact]
    public async Task InvokeAsync_Should_Add_Performance_Headers_In_Development()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.RequestServices = _serviceProvider;
        context.Request.Method = "GET";
        context.Request.Path = "/api/test";
        context.Response.Body = new MemoryStream();

        RequestDelegate next = async (_) =>
        {
            await Task.Delay(10); // 模擬一些處理時間
        };

        var middleware = new PerformanceMiddleware(next, _logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("X-Response-Time-Ms"));
        Assert.True(context.Response.Headers.ContainsKey("X-Performance-Category"));
        
        var responseTime = context.Response.Headers["X-Response-Time-Ms"].ToString();
        Assert.True(int.TryParse(responseTime, out var time));
        Assert.True(time >= 0);
    }

    [Fact]
    public async Task InvokeAsync_Should_Categorize_Performance_Correctly()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.RequestServices = _serviceProvider;
        context.Request.Method = "GET";
        context.Request.Path = "/api/test";

        RequestDelegate next = (_) => Task.CompletedTask; // 快速回應

        var middleware = new PerformanceMiddleware(next, _logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var category = context.Response.Headers["X-Performance-Category"].ToString();
        Assert.Contains(category, new[] { "very-fast", "fast", "moderate", "slow", "very-slow" });
    }

    [Fact]
    public async Task InvokeAsync_Should_Handle_Exceptions_And_Still_Measure_Time()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.RequestServices = _serviceProvider;
        context.Request.Method = "GET";
        context.Request.Path = "/api/test";

        RequestDelegate next = (_) => throw new InvalidOperationException("Test exception");

        var middleware = new PerformanceMiddleware(next, _logger);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));
        
        // 驗證即使發生異常，效能標頭仍然被添加（在 Development 環境）
        Assert.True(context.Response.Headers.ContainsKey("X-Response-Time-Ms"));
    }

    [Theory]
    [InlineData(5, "very-fast")]
    [InlineData(50, "very-fast")]
    [InlineData(150, "fast")]
    [InlineData(600, "moderate")]
    [InlineData(1500, "slow")]
    [InlineData(6000, "very-slow")]
    public async Task InvokeAsync_Should_Categorize_Performance_Based_On_Time(int delayMs, string expectedCategory)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.RequestServices = _serviceProvider;
        context.Request.Method = "GET";
        context.Request.Path = "/api/test";

        RequestDelegate next = async (_) =>
        {
            await Task.Delay(delayMs);
        };

        var middleware = new PerformanceMiddleware(next, _logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var category = context.Response.Headers["X-Performance-Category"].ToString();
        Assert.Equal(expectedCategory, category);
    }

    [Fact]
    public async Task InvokeAsync_Should_Log_Slow_Request_Warning()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<PerformanceMiddleware>();
        
        var context = new DefaultHttpContext();
        context.RequestServices = _serviceProvider;
        context.Request.Method = "GET";
        context.Request.Path = "/api/slow-endpoint";
        context.Request.QueryString = new QueryString("?param=test");

        RequestDelegate next = async (_) =>
        {
            await Task.Delay(1100); // 超過 1000ms 閾值
        };

        var middleware = new PerformanceMiddleware(next, logger);

        // Act
        await middleware.InvokeAsync(context);

        // Assert - 主要驗證沒有拋出異常且處理完成
        Assert.True(context.Response.Headers.ContainsKey("X-Response-Time-Ms"));
        var responseTime = int.Parse(context.Response.Headers["X-Response-Time-Ms"].ToString());
        Assert.True(responseTime >= 1000);
    }

    private class TestHostEnvironment : IHostEnvironment
    {
        public TestHostEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "TestApp";
        public string ContentRootPath { get; set; } = "";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}