using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Planboard.Api.Auth;
using Planboard.Api.Data;
using Planboard.Api.Domain;

namespace Planboard.Api.GraphQL;

[MutationType]
public static class ProjectMutations
{
    [Authorize(Policy = Policies.Writer)]
    [Error<ValidationException>]
    [Error<DuplicateNameException>]
    public static async Task<Project> CreateProjectAsync(
        string name,
        string? description,
        PlanboardDbContext db,
        CancellationToken cancellationToken)
    {
        var project = new Project
        {
            Id = Guid.CreateVersion7(),
            Name = TaskRules.RequireText("name", name, 200),
            Description = TaskRules.OptionalText("description", description, 2000),
            CreatedAt = DateTimeOffset.UtcNow
        };

        if (await db.Projects.AnyAsync(p => p.Name == project.Name, cancellationToken))
        {
            throw new DuplicateNameException(project.Name);
        }

        db.Projects.Add(project);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new DuplicateNameException(project.Name);
        }

        return project;
    }
}
