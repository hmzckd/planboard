using Microsoft.EntityFrameworkCore;

namespace Planboard.Api.Data;

public static class DatabaseSetup
{
    public static IServiceCollection AddPlanboardDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("App")
            ?? throw new InvalidOperationException("Connection string 'App' is missing.");

        services.AddPooledDbContextFactory<PlanboardDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());

        return services;
    }

    public static async Task MigrateAsync(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Owner")
            ?? throw new InvalidOperationException("Connection string 'Owner' is missing.");

        var options = new DbContextOptionsBuilder<PlanboardDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        await using var db = new PlanboardDbContext(options);
        await db.Database.MigrateAsync();
    }
}
