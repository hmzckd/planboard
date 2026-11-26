namespace Planboard.Api.Data;

public sealed class Label
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public required string Name { get; set; }

    public required string Color { get; set; }
}

public sealed class TaskLabel
{
    public Guid TenantId { get; set; }

    public Guid TaskId { get; set; }

    public Guid LabelId { get; set; }
}
