using Microsoft.EntityFrameworkCore;
using Planboard.Api.Data;

namespace Planboard.Api.GraphQL;

[ObjectType<Project>]
public static partial class ProjectNode
{
    static partial void Configure(IObjectTypeDescriptor<Project> descriptor)
    {
        descriptor.Ignore(p => p.TenantId);
    }

    public static async Task<List<ProjectTask>> GetTasks(
        [Parent] Project project,
        PlanboardDbContext db,
        CancellationToken cancellationToken) =>
        await db.Tasks
            .Where(t => t.ProjectId == project.Id)
            .OrderBy(t => t.CreatedAt).ThenBy(t => t.Id)
            .ToListAsync(cancellationToken);
}
