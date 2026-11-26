namespace Planboard.Api.Domain;

public sealed class DuplicateEmailException(string email) : PlanboardException($"A user with the email '{email}' already exists.")
{
    public override string Code => "DUPLICATE_EMAIL";
}

public sealed class LastOwnerException() : PlanboardException("A tenant must keep at least one owner.")
{
    public override string Code => "LAST_OWNER";
}
