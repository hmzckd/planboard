using Microsoft.EntityFrameworkCore;
using Planboard.Api.Auth;

namespace Planboard.Api.Data;

public static class DevelopmentSeeder
{
    private static readonly (string Tenant, (string Name, Role Role)[] Users)[] Tenants =
    [
        ("Acme", [("Alice", Role.Owner), ("Bob", Role.Member), ("Carol", Role.Member), ("Dave", Role.Viewer)]),
        ("Globex", [("Grace", Role.Owner), ("Heidi", Role.Member), ("Ivan", Role.Member), ("Judy", Role.Viewer), ("Mallory", Role.Member)]),
        ("Initech", [("Peter", Role.Owner), ("Michael", Role.Member), ("Samir", Role.Member), ("Milton", Role.Viewer)])
    ];

    public static string TokenFor(string firstName) => $"pb_dev_{firstName.ToLowerInvariant()}";

    public static async Task SeedAsync(PlanboardDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Tenants.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;

        foreach (var (tenantName, users) in Tenants)
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
}
