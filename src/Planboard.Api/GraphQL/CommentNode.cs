using Planboard.Api.Data;

namespace Planboard.Api.GraphQL;

[ObjectType<Comment>]
[Node]
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
        IUserByIdDataLoader userById,
        CancellationToken cancellationToken) =>
        await userById.LoadRequiredAsync(comment.AuthorId, cancellationToken);

    [NodeResolver]
    public static async Task<Comment?> GetCommentByIdAsync(
        Guid id,
        ICommentByIdDataLoader commentById,
        CancellationToken cancellationToken) =>
        await commentById.LoadAsync(id, cancellationToken);
}
