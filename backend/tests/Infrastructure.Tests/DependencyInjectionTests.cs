using System;
using System.Collections.Generic;
using AdhdProductivitySystem.Application.Common.Interfaces;
using AdhdProductivitySystem.Infrastructure;
using AdhdProductivitySystem.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AdhdProductivitySystem.Infrastructure.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_Should_Register_All_Services()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=test;Password=test"
            })
            .Build();

        // Act
        services.AddInfrastructure(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert - Verify services are registered
        Assert.NotNull(serviceProvider.GetService<ApplicationDbContext>());
        Assert.NotNull(serviceProvider.GetService<IUnitOfWork>());
        Assert.NotNull(serviceProvider.GetService<ITaskRepository>());
        Assert.NotNull(serviceProvider.GetService<IRepository<Domain.Entities.TaskItem>>());
    }

    [Fact]
    public void Repository_Should_Be_Scoped_Service()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=test;Password=test"
            })
            .Build();

        services.AddInfrastructure(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        using (var scope1 = serviceProvider.CreateScope())
        using (var scope2 = serviceProvider.CreateScope())
        {
            var repo1 = scope1.ServiceProvider.GetService<ITaskRepository>();
            var repo2 = scope2.ServiceProvider.GetService<ITaskRepository>();
            var repoSameScope = scope1.ServiceProvider.GetService<ITaskRepository>();

            // Assert - Different scopes should have different instances
            Assert.NotSame(repo1, repo2);
            // Same scope should have same instance
            Assert.Same(repo1, repoSameScope);
        }
    }

    [Fact]
    public void UnitOfWork_Should_Provide_Same_Repository_Instances()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=test;Password=test"
            })
            .Build();

        services.AddInfrastructure(configuration);
        var serviceProvider = services.BuildServiceProvider();

        using (var scope = serviceProvider.CreateScope())
        {
            // Act
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var tasksRepo1 = unitOfWork.Tasks;
            var tasksRepo2 = unitOfWork.Tasks;

            // Assert - UnitOfWork should return same repository instance
            Assert.Same(tasksRepo1, tasksRepo2);
        }
    }
}