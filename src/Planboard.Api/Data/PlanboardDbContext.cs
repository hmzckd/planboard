using Microsoft.EntityFrameworkCore;
using Planboard.Api.Diagnostics;

namespace Planboard.Api.Data;

public sealed class PlanboardDbContext(DbContextOptions<PlanboardDbContext> options) : DbContext(options)
{
    public Guid? TenantId { get; set; }

    public SqlCommandCounter? CommandCounter { get; set; }

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<User> Users => Set<User>();

    public DbSet<ApiToken> ApiTokens => Set<ApiToken>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<ProjectTask> Tasks => Set<ProjectTask>();

    public DbSet<Comment> Comments => Set<Comment>();

    public DbSet<Label> Labels => Set<Label>();

    public DbSet<TaskLabel> TaskLabels => Set<TaskLabel>();

    public override void Dispose()
    {
        TenantId = null;
        CommandCounter = null;
        base.Dispose();
    }

    public override ValueTask DisposeAsync()
    {
        TenantId = null;
        CommandCounter = null;
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
            user.HasAlternateKey(u => new { u.TenantId, u.Id });
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
            project.HasAlternateKey(p => new { p.TenantId, p.Id });
            project.HasOne<Tenant>().WithMany().HasForeignKey(p => p.TenantId);
            project.HasQueryFilter(p => p.TenantId == TenantId);
        });

        modelBuilder.Entity<ProjectTask>(task =>
        {
            task.ToTable("tasks");
            task.Property(t => t.Id).ValueGeneratedNever();
            task.Property(t => t.Title).HasMaxLength(200);
            task.Property(t => t.Description).HasMaxLength(4000);
            task.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
            task.Property(t => t.Priority).HasConversion<string>().HasMaxLength(20);
            task.HasAlternateKey(t => new { t.TenantId, t.Id });
            task.HasIndex(t => new { t.ProjectId, t.CreatedAt, t.Id });
            task.HasOne<Project>().WithMany()
                .HasForeignKey(t => new { t.TenantId, t.ProjectId })
                .HasPrincipalKey(p => new { p.TenantId, p.Id })
                .OnDelete(DeleteBehavior.Cascade);
            task.HasOne<User>().WithMany()
                .HasForeignKey(t => new { t.TenantId, t.AssigneeId })
                .HasPrincipalKey(u => new { u.TenantId, u.Id })
                .OnDelete(DeleteBehavior.Restrict);
            task.HasQueryFilter(t => t.TenantId == TenantId);
        });

        modelBuilder.Entity<Comment>(comment =>
        {
            comment.Property(c => c.Id).ValueGeneratedNever();
            comment.Property(c => c.Body).HasMaxLength(4000);
            comment.HasIndex(c => new { c.TaskId, c.CreatedAt, c.Id });
            comment.HasOne<ProjectTask>().WithMany()
                .HasForeignKey(c => new { c.TenantId, c.TaskId })
                .HasPrincipalKey(t => new { t.TenantId, t.Id })
                .OnDelete(DeleteBehavior.Cascade);
            comment.HasOne<User>().WithMany()
                .HasForeignKey(c => new { c.TenantId, c.AuthorId })
                .HasPrincipalKey(u => new { u.TenantId, u.Id })
                .OnDelete(DeleteBehavior.Restrict);
            comment.HasQueryFilter(c => c.TenantId == TenantId);
        });

        modelBuilder.Entity<Label>(label =>
        {
            label.Property(l => l.Id).ValueGeneratedNever();
            label.Property(l => l.Name).HasMaxLength(50);
            label.Property(l => l.Color).HasMaxLength(7);
            label.HasIndex(l => new { l.TenantId, l.Name }).IsUnique();
            label.HasAlternateKey(l => new { l.TenantId, l.Id });
            label.HasOne<Tenant>().WithMany().HasForeignKey(l => l.TenantId);
            label.HasQueryFilter(l => l.TenantId == TenantId);
        });

        modelBuilder.Entity<TaskLabel>(taskLabel =>
        {
            taskLabel.HasKey(tl => new { tl.TaskId, tl.LabelId });
            taskLabel.HasOne<ProjectTask>().WithMany()
                .HasForeignKey(tl => new { tl.TenantId, tl.TaskId })
                .HasPrincipalKey(t => new { t.TenantId, t.Id })
                .OnDelete(DeleteBehavior.Cascade);
            taskLabel.HasOne<Label>().WithMany()
                .HasForeignKey(tl => new { tl.TenantId, tl.LabelId })
                .HasPrincipalKey(l => new { l.TenantId, l.Id })
                .OnDelete(DeleteBehavior.Cascade);
            taskLabel.HasQueryFilter(tl => tl.TenantId == TenantId);
        });
    }
}
