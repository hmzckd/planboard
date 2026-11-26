using Microsoft.EntityFrameworkCore;
using Planboard.Api.Auth;

namespace Planboard.Api.Data;

public static class DevelopmentSeeder
{
    private static readonly (string Tenant, (string Name, Role Role)[] Users, string[] Projects)[] Tenants =
    [
        ("Acme",
            [("Alice", Role.Owner), ("Bob", Role.Member), ("Carol", Role.Member), ("Dave", Role.Viewer)],
            ["Website redesign", "Mobile app", "Billing migration", "Customer portal", "Data warehouse", "Security audit", "Onboarding flow"]),
        ("Globex",
            [("Grace", Role.Owner), ("Heidi", Role.Member), ("Ivan", Role.Member), ("Judy", Role.Viewer), ("Mallory", Role.Member)],
            ["Warehouse robots", "Supplier API", "Mobile app", "Annual report", "Office move", "Support chatbot", "Price engine"]),
        ("Initech",
            [("Peter", Role.Owner), ("Michael", Role.Member), ("Samir", Role.Member), ("Milton", Role.Viewer)],
            ["TPS reports", "Printer fleet", "Legacy cleanup", "Payroll system", "Intranet", "Y2K follow-up"])
    ];

    public static string TokenFor(string firstName) => $"pb_dev_{firstName.ToLowerInvariant()}";

    public static async Task SeedAsync(PlanboardDbContext db, CancellationToken cancellationToken = default)
    {
        await SeedTenantsAndUsersAsync(db, cancellationToken);
        await SeedProjectsAsync(db, cancellationToken);
    }

    private static async Task SeedTenantsAndUsersAsync(PlanboardDbContext db, CancellationToken cancellationToken)
    {
        if (await db.Tenants.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;

        foreach (var (tenantName, users, _) in Tenants)
        {
            var tenant = new Tenant { Id = Guid.CreateVersion7(), Name = tenantName, CreatedAt = now };
            db.Tenants.Add(tenant);

            foreach (var (name, role) in users)
            {
                var user = new User
                {
                    Id = Guid.CreateVersion7(),
                    TenantId = tenant.Id,
                    Email = $"{name.ToLowerInvariant()}@{tenantName.ToLowerInvariant()}.test",
                    DisplayName = name,
                    Role = role,
                    CreatedAt = now
                };
                db.Users.Add(user);

                db.ApiTokens.Add(new ApiToken
                {
                    Id = Guid.CreateVersion7(),
                    UserId = user.Id,
                    TokenHash = ApiTokenHasher.Hash(TokenFor(name)),
                    Name = "Development token",
                    CreatedAt = now
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedProjectsAsync(PlanboardDbContext db, CancellationToken cancellationToken)
    {
        if (await db.Projects.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            return;
        }

        var tenants = await db.Tenants.IgnoreQueryFilters().ToDictionaryAsync(t => t.Name, t => t.Id, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        foreach (var (tenantName, _, projects) in Tenants)
        {
            foreach (var projectName in projects)
            {
                db.Projects.Add(new Project
                {
                    Id = Guid.CreateVersion7(),
                    TenantId = tenants[tenantName],
                    Name = projectName,
                    Description = $"{projectName} at {tenantName}.",
                    CreatedAt = now
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
