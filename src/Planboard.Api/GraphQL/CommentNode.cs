using Microsoft.EntityFrameworkCore;
using Planboard.Api.Data;

namespace Planboard.Api.GraphQL;

[ObjectType<Comment>]
public static partial class CommentNode
{
    static partial void Configure(IObjectTypeDescriptor<Comment> descriptor)
    {
        descriptor.Ignore(c => c.TenantId);
        descriptor.Ignore(c => c.TaskId);
        descriptor.Ignore(c => c.AuthorId);
    }

    public static async Task<User> GetAuthor(
        [Parent] Comment comment,
        PlanboardDbContext db,
        CancellationToken cancellationToken) =>
        await db.Users.SingleAsync(u => u.Id == comment.AuthorId, cancellationToken);
}
