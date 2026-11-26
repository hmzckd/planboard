using Microsoft.EntityFrameworkCore;
using Planboard.Api.Data;

namespace Planboard.Api.GraphQL;

[ObjectType<User>]
[Node]
public static partial class UserNode
{
    static partial void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor.Ignore(u => u.TenantId);
    }

    public static async Task<Tenant> GetTenant(
        [Parent] User user,
        PlanboardDbContext db,
        CancellationToken cancellationToken) =>
        await db.Tenants.SingleAsync(t => t.Id == user.TenantId, cancellationToken);

    [NodeResolver]
    public static async Task<User?> GetUserByIdAsync(
        Guid id,
        IUserByIdDataLoader userById,
        CancellationToken cancellationToken) =>
        await userById.LoadAsync(id, cancellationToken);
}
