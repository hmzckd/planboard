using HotChocolate.Subscriptions;
using Planboard.Api.Auth;
using Planboard.Api.Data;

namespace Planboard.Api.GraphQL;

public enum TaskChangeKind
{
    Created,
    Updated,
    StatusChanged,
    Assigned,
    Commented
}

public sealed record TaskChangedEvent(Guid TaskId, Guid ProjectId, TaskChangeKind Kind, Guid ChangedById);

public static class TaskEvents
{
    public static string Topic(Guid projectId) => $"taskChanged:{projectId}";

    public static async ValueTask PublishAsync(
        this ITopicEventSender sender,
        ProjectTask task,
        TaskChangeKind kind,
        CurrentUser currentUser,
        CancellationToken cancellationToken) =>
        await sender.SendAsync(
            Topic(task.ProjectId),
            new TaskChangedEvent(task.Id, task.ProjectId, kind, currentUser.UserId),
            cancellationToken);
}

[ObjectType<TaskChangedEvent>]
public static partial class TaskChangedEventNode
{
    static partial void Configure(IObjectTypeDescriptor<TaskChangedEvent> descriptor)
    {
        descriptor.Ignore(e => e.TaskId);
        descriptor.Ignore(e => e.ProjectId);
        descriptor.Ignore(e => e.ChangedById);
    }

    public static async Task<ProjectTask?> GetTask(
        [Parent] TaskChangedEvent message,
        ITaskByIdDataLoader taskById,
        CancellationToken cancellationToken) =>
        await taskById.LoadAsync(message.TaskId, cancellationToken);

    public static async Task<User?> GetChangedBy(
        [Parent] TaskChangedEvent message,
        IUserByIdDataLoader userById,
        CancellationToken cancellationToken) =>
        await userById.LoadAsync(message.ChangedById, cancellationToken);
}
