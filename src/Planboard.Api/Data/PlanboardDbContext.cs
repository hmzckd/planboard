using Microsoft.EntityFrameworkCore;

namespace Planboard.Api.Data;

public sealed class PlanboardDbContext(DbContextOptions<PlanboardDbContext> options) : DbContext(options)
{
    public Guid? TenantId { get; set; }

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<User> Users => Set<User>();

    public DbSet<ApiToken> ApiTokens => Set<ApiToken>();

    public DbSet<Project> Projects => Set<Project>();

    public override void Dispose()
    {
        TenantId = null;
        base.Dispose();
    }

    public override ValueTask DisposeAsync()
    {
        TenantId = null;
        return base.DisposeAsync();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(tenant =>
        {
            tenant.Property(t => t.Id).ValueGeneratedNever();
            tenant.Property(t => t.Name).HasMaxLength(100);
            tenant.HasIndex(t => t.Name).IsUnique();
            tenant.HasQueryFilter(t => t.Id == TenantId);
        });

        modelBuilder.Entity<User>(user =>
        {
            user.Property(u => u.Id).ValueGeneratedNever();
            user.Property(u => u.Email).HasMaxLength(254);
            user.Property(u => u.DisplayName).HasMaxLength(100);
            user.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
            user.HasIndex(u => u.Email).IsUnique();
            user.HasOne<Tenant>().WithMany().HasForeignKey(u => u.TenantId);
            user.HasQueryFilter(u => u.TenantId == TenantId);
        });

        modelBuilder.Entity<ApiToken>(token =>
        {
            token.Property(t => t.Id).ValueGeneratedNever();
            token.Property(t => t.Name).HasMaxLength(100);
            token.HasIndex(t => t.TokenHash).IsUnique();
            token.HasOne<User>().WithMany().HasForeignKey(t => t.UserId);
        });

        modelBuilder.Entity<Project>(project =>
        {
            project.Property(p => p.Id).ValueGeneratedNever();
            project.Property(p => p.Name).HasMaxLength(200);
            project.Property(p => p.Description).HasMaxLength(2000);
            project.HasIndex(p => new { p.TenantId, p.Name }).IsUnique();
            project.HasOne<Tenant>().WithMany().HasForeignKey(p => p.TenantId);
            project.HasQueryFilter(p => p.TenantId == TenantId);
        });
    }
}
