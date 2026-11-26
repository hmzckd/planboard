using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;
using Planboard.Api.Auth;
using Planboard.Api.Data;

namespace Planboard.Api.GraphQL;

[QueryType]
public static class Query
{
    public static string Ping() => "pong";

    [Authorize]
    public static async Task<User?> GetMe(
        CurrentUser currentUser,
        PlanboardDbContext db,
        CancellationToken cancellationToken) =>
        await db.Users.FirstOrDefaultAsync(u => u.Id == currentUser.UserId, cancellationToken);
}
