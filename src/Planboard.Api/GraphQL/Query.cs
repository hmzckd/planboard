using GreenDonut.Data;
using HotChocolate.Authorization;
using HotChocolate.Types.Pagination;
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
        IUserByIdDataLoader userById,
        CancellationToken cancellationToken) =>
        await userById.LoadAsync(currentUser.UserId, cancellationToken);

    [Authorize]
    [UsePaging]
    public static async Task<Connection<Project>> GetProjectsAsync(
        PagingArguments pagingArguments,
        PlanboardDbContext db,
        CancellationToken cancellationToken) =>
        await db.Projects
            .OrderBy(p => p.Name).ThenBy(p => p.Id)
            .ToPageAsync(pagingArguments, cancellationToken)
            .ToConnectionAsync();

    [Authorize]
    public static async Task<Project?> GetProjectAsync(
        [ID<Project>] Guid id,
        IProjectByIdDataLoader projectById,
        CancellationToken cancellationToken) =>
        await projectById.LoadAsync(id, cancellationToken);
}
