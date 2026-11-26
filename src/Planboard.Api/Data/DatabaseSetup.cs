using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql;
using Planboard.Api.Diagnostics;

namespace Planboard.Api.Data;

public static class DatabaseSetup
{
    public static IServiceCollection AddPlanboardDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("App")
            ?? throw new InvalidOperationException("Connection string 'App' is missing.");

        services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));

        services.AddSingleton(serviceProvider => new PooledDbContextFactory<PlanboardDbContext>(
            CreateAppOptions(serviceProvider.GetRequiredService<NpgsqlDataSource>())));

        services.AddScoped<IDbContextFactory<PlanboardDbContext>, TenantDbContextFactory>();

        return services;
    }

    public static DbContextOptions<PlanboardDbContext> CreateAppOptions(NpgsqlDataSource dataSource) =>
        new DbContextOptionsBuilder<PlanboardDbContext>()
            .UseNpgsql(dataSource)
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(new TenantConnectionInterceptor(), new SqlCommandCountingInterceptor())
            .Options;

    public static async Task PrepareAsync(IConfiguration configuration)
    {
        var migrate = configuration.GetValue<bool>("Database:MigrateOnStartup");
        var seed = configuration.GetValue<bool>("Database:SeedOnStartup");

        if (!migrate && !seed)
        {
            return;
        }

        var connectionString = configuration.GetConnectionString("Owner")
            ?? throw new InvalidOperationException("Connection string 'Owner' is missing.");

        var options = new DbContextOptionsBuilder<PlanboardDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        await using var db = new PlanboardDbContext(options);

        if (migrate)
        {
            await db.Database.MigrateAsync();
        }

        if (seed)
        {
            await DevelopmentSeeder.SeedAsync(db);
        }
    }
}
