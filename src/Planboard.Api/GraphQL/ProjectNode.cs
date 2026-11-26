using Planboard.Api.Data;

namespace Planboard.Api.GraphQL;

[ObjectType<Project>]
public static partial class ProjectNode
{
    static partial void Configure(IObjectTypeDescriptor<Project> descriptor)
    {
        descriptor.Ignore(p => p.TenantId);
    }

    public static async Task<ProjectTask[]> GetTasks(
        [Parent] Project project,
        ITasksByProjectIdDataLoader tasksByProjectId,
        CancellationToken cancellationToken) =>
        await tasksByProjectId.LoadAsync(project.Id, cancellationToken) ?? [];
}
