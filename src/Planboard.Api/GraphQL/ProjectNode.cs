using GreenDonut.Data;
using HotChocolate.Types.Pagination;
using Planboard.Api.Data;

namespace Planboard.Api.GraphQL;

[ObjectType<Project>]
[Node]
public static partial class ProjectNode
{
    static partial void Configure(IObjectTypeDescriptor<Project> descriptor)
    {
        descriptor.Ignore(p => p.TenantId);
    }

    [UsePaging]
    public static async Task<Connection<ProjectTask>> GetTasksAsync(
        [Parent] Project project,
        ProjectTaskStatus? status,
        PagingArguments pagingArguments,
        ITasksByProjectIdDataLoader tasksByProjectId,
        CancellationToken cancellationToken)
    {
        var query = new QueryContext<ProjectTask>(Predicate: status is { } s ? t => t.Status == s : null);

        return await tasksByProjectId
            .With(pagingArguments, query)
            .LoadAsync(project.Id, cancellationToken)
            .ToConnectionAsync();
    }

    [NodeResolver]
    public static async Task<Project?> GetProjectByIdAsync(
        Guid id,
        IProjectByIdDataLoader projectById,
        CancellationToken cancellationToken) =>
        await projectById.LoadAsync(id, cancellationToken);
}
