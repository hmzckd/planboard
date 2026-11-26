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
    public static async Task<ILookup<Guid, ProjectTask>> GetTasksByProjectIdAsync(
        IReadOnlyList<Guid> projectIds,
        IDbContextFactory<PlanboardDbContext> dbFactory,
        CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var tasks = await db.Tasks
            .Where(t => projectIds.Contains(t.ProjectId))
            .OrderBy(t => t.CreatedAt).ThenBy(t => t.Id)
            .ToListAsync(cancellationToken);
        return tasks.ToLookup(t => t.ProjectId);
    }

    [DataLoader]
    public static async Task<ILookup<Guid, Comment>> GetCommentsByTaskIdAsync(
        IReadOnlyList<Guid> taskIds,
        IDbContextFactory<PlanboardDbContext> dbFactory,
        CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var comments = await db.Comments
            .Where(c => taskIds.Contains(c.TaskId))
            .OrderBy(c => c.CreatedAt).ThenBy(c => c.Id)
            .ToListAsync(cancellationToken);
        return comments.ToLookup(c => c.TaskId);
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
