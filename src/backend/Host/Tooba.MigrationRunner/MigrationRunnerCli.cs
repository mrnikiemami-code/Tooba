namespace Tooba.MigrationRunner;

/// <summary>
/// پارامترهای CLI runner.
/// </summary>
internal sealed class MigrationRunnerCli
{
    public MigrationRunnerCommand Command { get; init; } = MigrationRunnerCommand.Status;

    public IReadOnlyList<string> TenantIds { get; init; } = [];

    public bool AllTenants { get; init; }

    public static bool TryParse(string[] args, out MigrationRunnerCli cli, out string? error)
    {
        cli = new MigrationRunnerCli();
        if (args.Length == 0)
        {
            error = "Usage: Tooba.MigrationRunner <status|plan|apply> [--tenant id] [--tenants id,id] [--all-tenants]";
            return false;
        }

        if (!Enum.TryParse<MigrationRunnerCommand>(args[0], ignoreCase: true, out var command))
        {
            error = $"Unknown command '{args[0]}'. Expected status, plan, or apply.";
            return false;
        }

        var tenantIds = new List<string>();
        var allTenants = false;

        for (var i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--tenant" when i + 1 < args.Length:
                    tenantIds.Add(args[++i]);
                    break;
                case "--tenants" when i + 1 < args.Length:
                    tenantIds.AddRange(args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                    break;
                case "--all-tenants":
                    allTenants = true;
                    break;
                default:
                    error = $"Unknown argument '{args[i]}'.";
                    return false;
            }
        }

        if (allTenants && tenantIds.Count > 0)
        {
            error = "Use either --all-tenants or explicit --tenant/--tenants, not both.";
            return false;
        }

        cli = new MigrationRunnerCli
        {
            Command = command,
            TenantIds = tenantIds,
            AllTenants = allTenants,
        };
        error = null;
        return true;
    }
}
