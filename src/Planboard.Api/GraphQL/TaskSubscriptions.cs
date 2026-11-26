using HotChocolate.Authorization;
using HotChocolate.Execution;
using HotChocolate.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Planboard.Api.Data;

namespace Planboard.Api.GraphQL;

[SubscriptionType]
public static class TaskSubscriptions
{
    public static async ValueTask<ISourceStream<TaskChangedEvent>> SubscribeToTaskChangedAsync(
        [ID<Project>] Guid projectId,
        PlanboardDbContext db,
        ITopicEventReceiver receiver,
        CancellationToken cancellationToken)
    {
        if (!await db.Projects.AnyAsync(p => p.Id == projectId, cancellationToken))
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("The project was not found.")
                    .SetCode("NOT_FOUND")
                    .Build());
        }

        return await receiver.SubscribeAsync<TaskChangedEvent>(TaskEvents.Topic(projectId), cancellationToken);
    }

    [Authorize]
    [Subscribe(With = nameof(SubscribeToTaskChangedAsync))]
    public static TaskChangedEvent TaskChanged(
        [ID<Project>] Guid projectId,
        [EventMessage] TaskChangedEvent message) =>
        message;
}
