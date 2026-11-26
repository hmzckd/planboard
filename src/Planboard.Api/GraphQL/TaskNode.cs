using Microsoft.EntityFrameworkCore;
using Planboard.Api.Data;

namespace Planboard.Api.GraphQL;

[ObjectType<ProjectTask>]
public static partial class TaskNode
{
    static partial void Configure(IObjectTypeDescriptor<ProjectTask> descriptor)
    {
        descriptor.Ignore(t => t.TenantId);
        descriptor.Ignore(t => t.ProjectId);
        descriptor.Ignore(t => t.AssigneeId);
    }

    public static async Task<Project> GetProject(
        [Parent] ProjectTask task,
        PlanboardDbContext db,
        CancellationToken cancellationToken) =>
        await db.Projects.SingleAsync(p => p.Id == task.ProjectId, cancellationToken);

    public static async Task<User?> GetAssignee(
        [Parent] ProjectTask task,
        PlanboardDbContext db,
        CancellationToken cancellationToken) =>
        task.AssigneeId is null
            ? null
            : await db.Users.SingleOrDefaultAsync(u => u.Id == task.AssigneeId, cancellationToken);

    public static async Task<List<Comment>> GetComments(
        [Parent] ProjectTask task,
        PlanboardDbContext db,
        CancellationToken cancellationToken) =>
        await db.Comments
            .Where(c => c.TaskId == task.Id)
            .OrderBy(c => c.CreatedAt).ThenBy(c => c.Id)
            .ToListAsync(cancellationToken);

    public static async Task<List<Label>> GetLabels(
        [Parent] ProjectTask task,
        PlanboardDbContext db,
        CancellationToken cancellationToken) =>
        await db.TaskLabels
            .Where(tl => tl.TaskId == task.Id)
            .Join(db.Labels, tl => tl.LabelId, l => l.Id, (_, l) => l)
            .OrderBy(l => l.Name)
            .ToListAsync(cancellationToken);
}
