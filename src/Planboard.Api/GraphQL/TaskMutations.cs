using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;
using Planboard.Api.Auth;
using Planboard.Api.Data;
using Planboard.Api.Domain;

namespace Planboard.Api.GraphQL;

[MutationType]
public static class TaskMutations
{
    [Authorize]
    [Error<ValidationException>]
    [Error<NotFoundException>]
    public static async Task<ProjectTask> CreateTaskAsync(
        [ID<Project>] Guid projectId,
        string title,
        string? description,
        ProjectTaskPriority? priority,
        [ID<User>] Guid? assigneeId,
        DateOnly? dueDate,
        PlanboardDbContext db,
        CancellationToken cancellationToken)
    {
        if (!await db.Projects.AnyAsync(p => p.Id == projectId, cancellationToken))
        {
            throw new NotFoundException("Project");
        }

        await EnsureUserExistsAsync(db, assigneeId, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var task = new ProjectTask
        {
            Id = Guid.CreateVersion7(),
            ProjectId = projectId,
            Title = TaskRules.RequireText("title", title, 200),
            Description = TaskRules.OptionalText("description", description, 4000),
            Status = ProjectTaskStatus.Todo,
            Priority = priority ?? ProjectTaskPriority.Medium,
            AssigneeId = assigneeId,
            DueDate = dueDate,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Tasks.Add(task);
        await db.SaveChangesAsync(cancellationToken);
        return task;
    }

    [Authorize]
    [Error<ValidationException>]
    [Error<NotFoundException>]
    public static async Task<ProjectTask> UpdateTaskAsync(
        [ID<ProjectTask>] Guid id,
        string? title,
        string? description,
        ProjectTaskPriority? priority,
        DateOnly? dueDate,
        PlanboardDbContext db,
        CancellationToken cancellationToken)
    {
        var task = await FindTaskAsync(db, id, cancellationToken);

        if (title is not null)
        {
            task.Title = TaskRules.RequireText("title", title, 200);
        }

        if (description is not null)
        {
            task.Description = TaskRules.OptionalText("description", description, 4000);
        }

        task.Priority = priority ?? task.Priority;
        task.DueDate = dueDate ?? task.DueDate;
        task.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return task;
    }

    [Authorize]
    [Error<NotFoundException>]
    [Error<InvalidStatusTransitionException>]
    public static async Task<ProjectTask> ChangeTaskStatusAsync(
        [ID<ProjectTask>] Guid id,
        ProjectTaskStatus status,
        PlanboardDbContext db,
        CancellationToken cancellationToken)
    {
        var task = await FindTaskAsync(db, id, cancellationToken);

        TaskRules.EnsureCanMove(task.Status, status);
        task.Status = status;
        task.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return task;
    }

    [Authorize]
    [Error<NotFoundException>]
    public static async Task<ProjectTask> AssignTaskAsync(
        [ID<ProjectTask>] Guid id,
        [ID<User>] Guid? assigneeId,
        PlanboardDbContext db,
        CancellationToken cancellationToken)
    {
        var task = await FindTaskAsync(db, id, cancellationToken);
        await EnsureUserExistsAsync(db, assigneeId, cancellationToken);

        task.AssigneeId = assigneeId;
        task.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return task;
    }

    [Authorize]
    [Error<ValidationException>]
    [Error<NotFoundException>]
    public static async Task<Comment> AddCommentAsync(
        [ID<ProjectTask>] Guid taskId,
        string body,
        CurrentUser currentUser,
        PlanboardDbContext db,
        CancellationToken cancellationToken)
    {
        await FindTaskAsync(db, taskId, cancellationToken);

        var comment = new Comment
        {
            Id = Guid.CreateVersion7(),
            TaskId = taskId,
            AuthorId = currentUser.UserId,
            Body = TaskRules.RequireText("body", body, 4000),
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Comments.Add(comment);
        await db.SaveChangesAsync(cancellationToken);
        return comment;
    }

    private static async Task<ProjectTask> FindTaskAsync(PlanboardDbContext db, Guid id, CancellationToken cancellationToken) =>
        await db.Tasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
        ?? throw new NotFoundException("Task");

    private static async Task EnsureUserExistsAsync(PlanboardDbContext db, Guid? userId, CancellationToken cancellationToken)
    {
        if (userId is { } id && !await db.Users.AnyAsync(u => u.Id == id, cancellationToken))
        {
            throw new NotFoundException("User");
        }
    }
}
