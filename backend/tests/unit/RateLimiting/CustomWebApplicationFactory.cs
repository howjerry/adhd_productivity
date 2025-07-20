using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using AdhdProductivitySystem.Infrastructure.Data;

namespace AdhdProductivitySystem.Tests.Unit.RateLimiting;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    static CustomWebApplicationFactory()
    {
        // 在任何其他代碼執行之前設定環境變數
        Environment.SetEnvironmentVariable("POSTGRES_PASSWORD", "TestPassword123!");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // 添加測試配置
            var inMemorySettings = new Dictionary<string, string>
            {
                ["JWT:SecretKey"] = "TestSecretKeyForTestingPurposesOnly12345678!@#$%^&*()_+{}[]|\\:;\"'<>,.?/~`",
                ["JWT:Issuer"] = "TestIssuer",
                ["JWT:Audience"] = "TestAudience",
                ["ConnectionStrings:DefaultConnection"] = "InMemory"
            };

            config.AddInMemoryCollection(inMemorySettings!);
        });

        builder.ConfigureServices(services =>
        {
            // 移除現有的 DbContext 註冊
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // 添加 InMemory 資料庫用於測試
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryDbForTesting");
            });
        });

        builder.UseEnvironment("Testing");
    }
}