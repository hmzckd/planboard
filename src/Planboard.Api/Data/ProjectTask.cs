using HotChocolate;

namespace Planboard.Api.Data;

[GraphQLName("Task")]
public sealed class ProjectTask
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid ProjectId { get; set; }

    public required string Title { get; set; }

    public string? Description { get; set; }

    public ProjectTaskStatus Status { get; set; }

    public ProjectTaskPriority Priority { get; set; }

    public Guid? AssigneeId { get; set; }

    public DateOnly? DueDate { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

[GraphQLName("TaskStatus")]
public enum ProjectTaskStatus
{
    Todo,
    InProgress,
    Done
}

[GraphQLName("TaskPriority")]
public enum ProjectTaskPriority
{
    Low,
    Medium,
    High
}
