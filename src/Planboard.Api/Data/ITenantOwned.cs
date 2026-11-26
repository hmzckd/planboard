namespace Planboard.Api.Data;

public interface ITenantOwned
{
    Guid TenantId { get; set; }
}
