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
        await SeedTasksAsync(db, cancellationToken);
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

    private static async Task SeedTasksAsync(PlanboardDbContext db, CancellationToken cancellationToken)
    {
        if (await db.Tasks.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            return;
        }

        var random = new Random(42);
        var start = DateTimeOffset.UtcNow.AddDays(-90);
        var users = await db.Users.IgnoreQueryFilters().ToListAsync(cancellationToken);
        var projects = await db.Projects.IgnoreQueryFilters().OrderBy(p => p.Name).ToListAsync(cancellationToken);
        var tenantIds = projects.Select(p => p.TenantId).Distinct().ToList();

        var labels = new Dictionary<Guid, List<Label>>();
        foreach (var tenantId in tenantIds)
        {
            labels[tenantId] = SeedData.Labels
                .Select(l => new Label { Id = Guid.CreateVersion7(), TenantId = tenantId, Name = l.Name, Color = l.Color })
                .ToList();
            db.Labels.AddRange(labels[tenantId]);
        }

        foreach (var project in projects)
        {
            var members = users.Where(u => u.TenantId == project.TenantId).ToList();
            var writers = members.Where(u => u.Role != Role.Viewer).ToList();

            for (var i = 0; i < TasksPerProject; i++)
            {
                var createdAt = start.AddMinutes(random.Next(0, 90 * 24 * 60));
                var task = new ProjectTask
                {
                    Id = Guid.CreateVersion7(createdAt),
                    TenantId = project.TenantId,
                    ProjectId = project.Id,
                    Title = $"{SeedData.Verbs[random.Next(SeedData.Verbs.Length)]} {SeedData.Subjects[random.Next(SeedData.Subjects.Length)]}",
                    Status = (ProjectTaskStatus)random.Next(3),
                    Priority = (ProjectTaskPriority)random.Next(3),
                    AssigneeId = random.Next(10) == 0 ? null : writers[random.Next(writers.Count)].Id,
                    DueDate = random.Next(3) == 0 ? null : DateOnly.FromDateTime(createdAt.AddDays(random.Next(3, 60)).UtcDateTime),
                    CreatedAt = createdAt,
                    UpdatedAt = createdAt
                };
                db.Tasks.Add(task);

                foreach (var label in labels[project.TenantId].OrderBy(_ => random.Next()).Take(random.Next(3)))
                {
                    db.TaskLabels.Add(new TaskLabel { TenantId = project.TenantId, TaskId = task.Id, LabelId = label.Id });
                }

                var commentCount = random.Next(4);
                for (var c = 0; c < commentCount; c++)
                {
                    var commentedAt = createdAt.AddHours(random.Next(1, 200));
                    db.Comments.Add(new Comment
                    {
                        Id = Guid.CreateVersion7(commentedAt),
                        TenantId = project.TenantId,
                        TaskId = task.Id,
                        AuthorId = members[random.Next(members.Count)].Id,
                        Body = SeedData.CommentBodies[random.Next(SeedData.CommentBodies.Length)],
                        CreatedAt = commentedAt
                    });
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private const int TasksPerProject = 100;
}
