using Planboard.Api.Data;

namespace Planboard.Api.Auth;

public static class Policies
{
    public const string Writer = "Writer";
    public const string Owner = "Owner";

    public static void AddPlanboardPolicies(this Microsoft.AspNetCore.Authorization.AuthorizationOptions options)
    {
        options.AddPolicy(Writer, policy => policy.RequireRole(nameof(Role.Owner), nameof(Role.Member)));
        options.AddPolicy(Owner, policy => policy.RequireRole(nameof(Role.Owner)));
    }
}
