namespace Planboard.Api.Data;

public sealed class ApiToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public required byte[] TokenHash { get; set; }

    public required string Name { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
