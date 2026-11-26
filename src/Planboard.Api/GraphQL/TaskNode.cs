using Planboard.Api.Data;

namespace Planboard.Api.GraphQL;

[ObjectType<ProjectTask>]
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

    public static async Task<Comment[]> GetComments(
        [Parent] ProjectTask task,
        ICommentsByTaskIdDataLoader commentsByTaskId,
        CancellationToken cancellationToken) =>
        await commentsByTaskId.LoadAsync(task.Id, cancellationToken) ?? [];

    public static async Task<Label[]> GetLabels(
        [Parent] ProjectTask task,
        ILabelsByTaskIdDataLoader labelsByTaskId,
        CancellationToken cancellationToken) =>
        await labelsByTaskId.LoadAsync(task.Id, cancellationToken) ?? [];
}
