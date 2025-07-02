// CRITICAL FIX: Replace line 36 in Program.cs with this PostgreSQL configuration
// This fixes the database provider mismatch issue

// OLD LINE 36 (INCORRECT):
// options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// NEW LINE 36 (CORRECT):
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ALSO ADD this using statement at the top of Program.cs:
// using Npgsql.EntityFrameworkCore.PostgreSQL;

// And add this NuGet package reference to AdhdProductivitySystem.Infrastructure.csproj:
// <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="7.0.11" />

// COMPLETE FIXED SECTION (lines 34-38):
/*
// Configure Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
*/