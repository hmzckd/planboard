using Planboard.Api.Data;

namespace Planboard.Api.Domain;

public static class TaskRules
{
    private static readonly HashSet<(ProjectTaskStatus From, ProjectTaskStatus To)> AllowedTransitions =
    [
        (ProjectTaskStatus.Todo, ProjectTaskStatus.InProgress),
        (ProjectTaskStatus.InProgress, ProjectTaskStatus.Todo),
        (ProjectTaskStatus.InProgress, ProjectTaskStatus.Done),
        (ProjectTaskStatus.Done, ProjectTaskStatus.InProgress)
    ];

    public static void EnsureCanMove(ProjectTaskStatus from, ProjectTaskStatus to)
    {
        if (from != to && !AllowedTransitions.Contains((from, to)))
        {
            throw new InvalidStatusTransitionException(Name(from), Name(to));
        }
    }

    public static string RequireText(string field, string? value, int maxLength)
    {
        var trimmed = value?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            throw new ValidationException(field, $"{field} is required.");
        }

        if (trimmed.Length > maxLength)
        {
            throw new ValidationException(field, $"{field} must be at most {maxLength} characters.");
        }

        return trimmed;
    }

    public static string? OptionalText(string field, string? value, int maxLength)
    {
        var trimmed = value?.Trim();

        if (trimmed?.Length > maxLength)
        {
            throw new ValidationException(field, $"{field} must be at most {maxLength} characters.");
        }

        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static string Name(ProjectTaskStatus status) => status switch
    {
        ProjectTaskStatus.Todo => "TODO",
        ProjectTaskStatus.InProgress => "IN_PROGRESS",
        _ => "DONE"
    };
}
