using GreenDonut.Data;
using Microsoft.EntityFrameworkCore;
using Planboard.Api.Data;

namespace Planboard.Api.GraphQL;

public static class DataLoaders
{
    [DataLoader]
    public static async Task<IReadOnlyDictionary<Guid, User>> GetUserByIdAsync(
        IReadOnlyList<Guid> ids,
        IDbContextFactory<PlanboardDbContext> dbFactory,
        CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Users
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);
    }

    [DataLoader]
    public static async Task<IReadOnlyDictionary<Guid, Project>> GetProjectByIdAsync(
        IReadOnlyList<Guid> ids,
        IDbContextFactory<PlanboardDbContext> dbFactory,
        CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Projects
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);
    }

    [DataLoader]
    public static async Task<IReadOnlyDictionary<Guid, ProjectTask>> GetTaskByIdAsync(
        IReadOnlyList<Guid> ids,
        IDbContextFactory<PlanboardDbContext> dbFactory,
        CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Tasks
            .Where(t => ids.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, cancellationToken);
    }

    [DataLoader]
    public static async Task<IReadOnlyDictionary<Guid, Comment>> GetCommentByIdAsync(
        IReadOnlyList<Guid> ids,
        IDbContextFactory<PlanboardDbContext> dbFactory,
        CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Comments
            .Where(c => ids.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);
    }

    [DataLoader]
    public static async Task<Dictionary<Guid, Page<ProjectTask>>> GetTasksByProjectIdAsync(
        IReadOnlyList<Guid> projectIds,
        PagingArguments pagingArguments,
        QueryContext<ProjectTask> query,
        IDbContextFactory<PlanboardDbContext> dbFactory,
        CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Tasks
            .Where(t => projectIds.Contains(t.ProjectId))
            .With(query, sort => sort.IfEmpty(s => s.AddAscending(t => t.CreatedAt).AddAscending(t => t.Id)))
            .ToBatchPageAsync(t => t.ProjectId, pagingArguments, cancellationToken);
    }

    [DataLoader]
    public static async Task<Dictionary<Guid, Page<Comment>>> GetCommentsByTaskIdAsync(
        IReadOnlyList<Guid> taskIds,
        PagingArguments pagingArguments,
        IDbContextFactory<PlanboardDbContext> dbFactory,
        CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Comments
            .Where(c => taskIds.Contains(c.TaskId))
            .OrderBy(c => c.CreatedAt).ThenBy(c => c.Id)
            .ToBatchPageAsync(c => c.TaskId, pagingArguments, cancellationToken);
    }

    [DataLoader]
    public static async Task<ILookup<Guid, Label>> GetLabelsByTaskIdAsync(
        IReadOnlyList<Guid> taskIds,
        IDbContextFactory<PlanboardDbContext> dbFactory,
        CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var rows = await db.TaskLabels
            .Where(tl => taskIds.Contains(tl.TaskId))
            .Join(db.Labels, tl => tl.LabelId, l => l.Id, (tl, l) => new { tl.TaskId, Label = l })
            .OrderBy(x => x.Label.Name)
            .ToListAsync(cancellationToken);
        return rows.ToLookup(x => x.TaskId, x => x.Label);
    }
}
