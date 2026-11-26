using System.Security.Claims;
using Planboard.Api.Data;

namespace Planboard.Api.Auth;

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor)
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.FindFirst(PlanboardClaims.TenantId) is not null;

    public Guid UserId => Guid.Parse(Require(PlanboardClaims.UserId));

    public Guid TenantId => Guid.Parse(Require(PlanboardClaims.TenantId));

    public Role Role => Enum.Parse<Role>(Require(PlanboardClaims.Role));

    private string Require(string claimType) =>
        Principal?.FindFirst(claimType)?.Value
        ?? throw new InvalidOperationException("There is no authenticated user in this request.");
}
