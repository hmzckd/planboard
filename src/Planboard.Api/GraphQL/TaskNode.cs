using GreenDonut.Data;
using HotChocolate.Types.Pagination;
using Planboard.Api.Data;

namespace Planboard.Api.GraphQL;

[ObjectType<ProjectTask>]
[Node]
public static partial class TaskNode
{
    static partial void Configure(IObjectTypeDescriptor<ProjectTask> descriptor)
    {
        descriptor.Ignore(t => t.TenantId);
        descriptor.Ignore(t => t.ProjectId);
        descriptor.Ignore(t => t.AssigneeId);
    }

    public static async Task<Project> GetProject(
        [Parent] ProjectTask task,
        IProjectByIdDataLoader projectById,
        CancellationToken cancellationToken) =>
        await projectById.LoadRequiredAsync(task.ProjectId, cancellationToken);

    public static async Task<User?> GetAssignee(
        [Parent] ProjectTask task,
        IUserByIdDataLoader userById,
        CancellationToken cancellationToken) =>
        task.AssigneeId is { } assigneeId
            ? await userById.LoadAsync(assigneeId, cancellationToken)
            : null;

    [UsePaging]
    public static async Task<Connection<Comment>> GetCommentsAsync(
        [Parent] ProjectTask task,
        PagingArguments pagingArguments,
        ICommentsByTaskIdDataLoader commentsByTaskId,
        CancellationToken cancellationToken) =>
        await commentsByTaskId
            .With(pagingArguments)
            .LoadAsync(task.Id, cancellationToken)
            .ToConnectionAsync();

    public static async Task<Label[]> GetLabels(
        [Parent] ProjectTask task,
        ILabelsByTaskIdDataLoader labelsByTaskId,
        CancellationToken cancellationToken) =>
        await labelsByTaskId.LoadAsync(task.Id, cancellationToken) ?? [];

    [NodeResolver]
    public static async Task<ProjectTask?> GetTaskByIdAsync(
        Guid id,
        ITaskByIdDataLoader taskById,
        CancellationToken cancellationToken) =>
        await taskById.LoadAsync(id, cancellationToken);
}
