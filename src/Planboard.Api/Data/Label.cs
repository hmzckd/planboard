namespace Planboard.Api.Data;

public sealed class Label : ITenantOwned
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public required string Name { get; set; }

    public required string Color { get; set; }
}

public sealed class TaskLabel : ITenantOwned
{
    public Guid TenantId { get; set; }

    public Guid TaskId { get; set; }

    public Guid LabelId { get; set; }
}
