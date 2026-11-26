namespace Planboard.Api.Domain;

public abstract class PlanboardException(string message) : Exception(message)
{
    public abstract string Code { get; }
}

public sealed class NotFoundException(string type) : PlanboardException($"{type} was not found.")
{
    public override string Code => "NOT_FOUND";
}

public sealed class ValidationException(string field, string message) : PlanboardException(message)
{
    public override string Code => "VALIDATION";

    public string Field { get; } = field;
}

public sealed class DuplicateNameException(string name) : PlanboardException($"A project named '{name}' already exists.")
{
    public override string Code => "DUPLICATE_NAME";
}

public sealed class InvalidStatusTransitionException(string from, string to)
    : PlanboardException($"A task cannot move from {from} to {to}.")
{
    public override string Code => "INVALID_TRANSITION";
}
