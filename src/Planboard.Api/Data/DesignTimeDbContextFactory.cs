using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Planboard.Api.Data;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PlanboardDbContext>
{
    public PlanboardDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PlanboardDbContext>()
            .UseNpgsql("Host=localhost;Port=5436;Database=planboard;Username=planboard_owner;Password=planboard_owner_dev")
            .UseSnakeCaseNamingConvention()
            .Options;

        return new PlanboardDbContext(options);
    }
}
