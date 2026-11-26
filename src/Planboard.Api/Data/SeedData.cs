namespace Planboard.Api.Data;

internal static class SeedData
{
    public static readonly string[] Verbs =
        ["Design", "Build", "Review", "Test", "Document", "Fix", "Refactor", "Plan", "Deploy", "Measure"];

    public static readonly string[] Subjects =
        ["login page", "search", "invoice export", "user settings", "API limits", "dashboard", "email templates",
         "audit log", "file upload", "notifications", "reports", "onboarding", "pricing table", "backups", "cache"];

    public static readonly string[] CommentBodies =
        ["Looks good to me.", "Can we split this into two tasks?", "Blocked by the API change.", "Done on staging.",
         "Please add a test for the edge case.", "I will pick this up tomorrow.", "Moved to next sprint.",
         "Customer asked about this again.", "Needs a design review.", "Merged, waiting for deploy."];

    public static readonly (string Name, string Color)[] Labels =
        [("bug", "#d73a4a"), ("feature", "#0e8a16"), ("chore", "#c5def5"), ("urgent", "#b60205"), ("design", "#5319e7")];
}
