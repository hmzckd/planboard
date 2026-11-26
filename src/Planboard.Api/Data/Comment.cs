namespace Planboard.Api.Data;

public sealed class Comment
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid TaskId { get; set; }

    public Guid AuthorId { get; set; }

    public required string Body { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
