using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;
using Planboard.Api.Auth;
using Planboard.Api.Data;
using Planboard.Api.Domain;

namespace Planboard.Api.GraphQL;

public sealed record InvitedMember(User User, string Token);

[MutationType]
public static class MemberMutations
{
    [Authorize(Policy = Policies.Owner)]
    [Error<ValidationException>]
    [Error<DuplicateEmailException>]
    public static async Task<InvitedMember> InviteMemberAsync(
        string email,
        string displayName,
        Role role,
        PlanboardDbContext db,
        ApiTokenIssuer tokenIssuer,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = TaskRules.RequireText("email", email, 254).ToLowerInvariant();

        if (!normalizedEmail.Contains('@'))
        {
            throw new ValidationException("email", "email must be an email address.");
        }

        if (await tokenIssuer.EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            throw new DuplicateEmailException(normalizedEmail);
        }

        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Email = normalizedEmail,
            DisplayName = TaskRules.RequireText("displayName", displayName, 100),
            Role = role,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        var token = await tokenIssuer.IssueAsync(db, user.Id, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return new InvitedMember(user, token);
    }

    [Authorize(Policy = Policies.Owner)]
    [Error<NotFoundException>]
    [Error<LastOwnerException>]
    public static async Task<User> ChangeMemberRoleAsync(
        [ID<User>] Guid userId,
        Role role,
        PlanboardDbContext db,
        CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException("User");

        if (user.Role == Role.Owner && role != Role.Owner
            && await db.Users.CountAsync(u => u.Role == Role.Owner, cancellationToken) == 1)
        {
            throw new LastOwnerException();
        }

        user.Role = role;
        await db.SaveChangesAsync(cancellationToken);
        return user;
    }
}
