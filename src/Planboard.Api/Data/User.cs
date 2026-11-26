namespace Planboard.Api.Data;

public sealed class User : ITenantOwned
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public required string Email { get; set; }

    public required string DisplayName { get; set; }

    public Role Role { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
