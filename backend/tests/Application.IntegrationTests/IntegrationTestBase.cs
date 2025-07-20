using System;
using System.Threading.Tasks;
using AdhdProductivitySystem.Application.Common.Interfaces;
using AdhdProductivitySystem.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AdhdProductivitySystem.Application.IntegrationTests;

/// <summary>
/// 整合測試基礎類別
/// </summary>
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected ServiceProvider ServiceProvider { get; private set; } = null!;
    protected ApplicationDbContext Context { get; private set; } = null!;
    protected ISender Sender { get; private set; } = null!;
    protected IUnitOfWork UnitOfWork { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();

        // 設定 DbContext 使用 InMemory 資料庫
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseInMemoryDatabase(Guid.NewGuid().ToString());
            options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
        });

        // 註冊 Application 層服務
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AdhdProductivitySystem.Application.Common.Interfaces.IApplicationDbContext).Assembly));

        // 註冊 Infrastructure 層服務
        services.AddScoped<IUnitOfWork, AdhdProductivitySystem.Infrastructure.Data.UnitOfWork>();
        services.AddScoped<ITaskRepository, AdhdProductivitySystem.Infrastructure.Data.TaskRepository>();
        services.AddScoped(typeof(IRepository<>), typeof(AdhdProductivitySystem.Infrastructure.Data.Repository<>));

        ServiceProvider = services.BuildServiceProvider();

        // 取得服務實例
        Context = ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Sender = ServiceProvider.GetRequiredService<ISender>();
        UnitOfWork = ServiceProvider.GetRequiredService<IUnitOfWork>();

        // 確保資料庫已創建
        await Context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await Context.DisposeAsync();
        await ServiceProvider.DisposeAsync();
    }

    /// <summary>
    /// 執行 MediatR 請求
    /// </summary>
    protected async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
    {
        using var scope = ServiceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(request);
    }
}