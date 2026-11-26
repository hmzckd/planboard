using Planboard.Api.Auth;
using Planboard.Api.Data;

namespace Planboard.Api.GraphQL;

[ObjectType<User>]
[Node]
public static partial class UserNode
{
    static partial void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor.Ignore(u => u.TenantId);
        descriptor.Ignore(u => u.Email);
    }

    public static string? GetEmail([Parent] User user, CurrentUser currentUser)
    {
        if (currentUser.Role != Role.Owner && currentUser.UserId != user.Id)
        {
            throw new GraphQLException(ErrorBuilder.New()
                .SetMessage("Only owners can see other members' email addresses.")
                .SetCode("AUTH_NOT_AUTHORIZED")
                .Build());
        }

        return user.Email;
    }

    public static async Task<Tenant> GetTenantAsync(
        [Parent] User user,
        ITenantByIdDataLoader tenantById,
        CancellationToken cancellationToken) =>
        await tenantById.LoadRequiredAsync(user.TenantId, cancellationToken);

    [NodeResolver]
    public static async Task<User?> GetUserByIdAsync(
        Guid id,
        IUserByIdDataLoader userById,
        CancellationToken cancellationToken) =>
        await userById.LoadAsync(id, cancellationToken);
}
