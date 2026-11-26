using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Planboard.Api.Auth;
using Planboard.Api.Diagnostics;

namespace Planboard.Api.Data;

public sealed class TenantDbContextFactory(
    PooledDbContextFactory<PlanboardDbContext> pooledFactory,
    CurrentUser currentUser,
    IHttpContextAccessor httpContextAccessor) : IDbContextFactory<PlanboardDbContext>
{
    public PlanboardDbContext CreateDbContext()
    {
        var db = pooledFactory.CreateDbContext();
        db.TenantId = currentUser.IsAuthenticated ? currentUser.TenantId : null;
        db.CommandCounter = httpContextAccessor.HttpContext?.Features.Get<SqlCommandCounter>();
        return db;
    }
}
