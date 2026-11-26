using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Planboard.Api.Auth;

namespace Planboard.Api.Data;

public sealed class TenantDbContextFactory(
    PooledDbContextFactory<PlanboardDbContext> pooledFactory,
    CurrentUser currentUser) : IDbContextFactory<PlanboardDbContext>
{
    public PlanboardDbContext CreateDbContext()
    {
        var db = pooledFactory.CreateDbContext();
        db.TenantId = currentUser.IsAuthenticated ? currentUser.TenantId : null;
        return db;
    }
}
