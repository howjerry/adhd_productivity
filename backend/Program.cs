using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AdhdProductivitySystem.Api.Middleware;

// 簡單的性能中間件測試
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== ADHD 生產力系統效能監控測試 ===\n");

        // 設置服務容器
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment("Development"));
        var serviceProvider = services.BuildServiceProvider();

        var logger = serviceProvider.GetRequiredService<ILogger<PerformanceMiddleware>>();

        // 測試 1: 正常請求
        Console.WriteLine("測試 1: 正常請求響應時間測量");
        await TestNormalRequest(logger, serviceProvider);

        // 測試 2: 慢請求
        Console.WriteLine("\n測試 2: 慢請求檢測");
        await TestSlowRequest(logger, serviceProvider);

        // 測試 3: 效能分類
        Console.WriteLine("\n測試 3: 效能分類測試");
        await TestPerformanceCategories(logger, serviceProvider);

        Console.WriteLine("\n=== 所有測試完成 ===");
    }

    static async Task TestNormalRequest(ILogger<PerformanceMiddleware> logger, IServiceProvider serviceProvider)
    {
        var context = CreateHttpContext(serviceProvider, "GET", "/api/test");
        var executed = false;

        RequestDelegate next = (ctx) =>
        {
            executed = true;
            return Task.CompletedTask;
        };

        var middleware = new PerformanceMiddleware(next, logger);
        var stopwatch = Stopwatch.StartNew();
        
        await middleware.InvokeAsync(context);
        
        stopwatch.Stop();
        
        Console.WriteLine($"  ✓ 請求執行: {executed}");
        Console.WriteLine($"  ✓ 實際耗時: {stopwatch.ElapsedMilliseconds}ms");
        
        if (context.Response.Headers.ContainsKey("X-Response-Time-Ms"))
        {
            var responseTime = context.Response.Headers["X-Response-Time-Ms"].ToString();
            Console.WriteLine($"  ✓ 記錄的響應時間: {responseTime}ms");
        }
        
        if (context.Response.Headers.ContainsKey("X-Performance-Category"))
        {
            var category = context.Response.Headers["X-Performance-Category"].ToString();
            Console.WriteLine($"  ✓ 效能分類: {category}");
        }
    }

    static async Task TestSlowRequest(ILogger<PerformanceMiddleware> logger, IServiceProvider serviceProvider)
    {
        var context = CreateHttpContext(serviceProvider, "POST", "/api/slow-endpoint");
        var executed = false;

        RequestDelegate next = async (ctx) =>
        {
            executed = true;
            await Task.Delay(1200); // 模擬慢請求
        };

        var middleware = new PerformanceMiddleware(next, logger);
        
        await middleware.InvokeAsync(context);
        
        Console.WriteLine($"  ✓ 慢請求執行: {executed}");
        
        if (context.Response.Headers.ContainsKey("X-Response-Time-Ms"))
        {
            var responseTime = int.Parse(context.Response.Headers["X-Response-Time-Ms"].ToString());
            Console.WriteLine($"  ✓ 響應時間: {responseTime}ms (應該 > 1000ms)");
            Console.WriteLine($"  ✓ 慢請求檢測: {(responseTime > 1000 ? "通過" : "失敗")}");
        }
    }

    static async Task TestPerformanceCategories(ILogger<PerformanceMiddleware> logger, IServiceProvider serviceProvider)
    {
        var testCases = new[]
        {
            new { Delay = 5, Expected = "very-fast" },
            new { Delay = 150, Expected = "fast" },
            new { Delay = 600, Expected = "moderate" },
            new { Delay = 1500, Expected = "slow" }
        };

        foreach (var testCase in testCases)
        {
            var context = CreateHttpContext(serviceProvider, "GET", $"/api/test/{testCase.Delay}");
            
            RequestDelegate next = async (ctx) =>
            {
                await Task.Delay(testCase.Delay);
            };

            var middleware = new PerformanceMiddleware(next, logger);
            await middleware.InvokeAsync(context);
            
            if (context.Response.Headers.ContainsKey("X-Performance-Category"))
            {
                var category = context.Response.Headers["X-Performance-Category"].ToString();
                var result = category == testCase.Expected ? "✓" : "✗";
                Console.WriteLine($"  {result} {testCase.Delay}ms -> {category} (期望: {testCase.Expected})");
            }
        }
    }

    static DefaultHttpContext CreateHttpContext(IServiceProvider serviceProvider, string method, string path)
    {
        var context = new DefaultHttpContext();
        context.RequestServices = serviceProvider;
        context.Request.Method = method;
        context.Request.Path = new PathString(path);
        context.Response.Body = new MemoryStream();
        return context;
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