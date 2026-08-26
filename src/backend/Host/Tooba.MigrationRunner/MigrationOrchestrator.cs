using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tooba.BuildingBlocks;
using Tooba.Host;

namespace Tooba.MigrationRunner;

/// <summary>
/// نتیجهٔ یک ماژول برای status/plan/apply.
/// </summary>
internal sealed record ModuleMigrationState(
    string Module,
    string Schema,
    string? CurrentMigration,
    IReadOnlyList<string> PendingMigrations,
    bool Succeeded,
    string? ErrorCode,
    TimeSpan Duration);

/// <summary>
/// status / plan / apply ماژول‌ها روی یک هدف پایگاه.
/// </summary>
internal sealed class MigrationOrchestrator
{
    private readonly DatabaseConnectionResolver _connectionResolver;
    private readonly ILogger<MigrationOrchestrator> _logger;

    public MigrationOrchestrator(DatabaseConnectionResolver connectionResolver, ILogger<MigrationOrchestrator> logger)
    {
        _connectionResolver = connectionResolver;
        _logger = logger;
    }

    internal async Task<IReadOnlyList<ModuleMigrationState>> RunAsync(
        MigrationTarget target,
        MigrationRunnerCommand command,
        CancellationToken cancellationToken)
    {
        var connectionString = _connectionResolver.Resolve(new ConnectionReference(target.ConnectionReference));
        var traceId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
        var states = new List<ModuleMigrationState>();

        foreach (var descriptor in ModuleMigrationRegistry.All)
        {
            var started = Stopwatch.StartNew();
            try
            {
                await using var context = descriptor.CreateContext(connectionString);
                var applied = (await context.Database.GetAppliedMigrationsAsync(cancellationToken)).ToList();
                var pending = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
                var current = applied.LastOrDefault();

                if (command == MigrationRunnerCommand.Apply && pending.Count > 0)
                {
                    await context.Database.MigrateAsync(cancellationToken);
                    applied = (await context.Database.GetAppliedMigrationsAsync(cancellationToken)).ToList();
                    pending = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
                    current = applied.LastOrDefault();
                }

                var state = new ModuleMigrationState(
                    descriptor.Module,
                    descriptor.Schema,
                    current,
                    pending,
                    Succeeded: true,
                    ErrorCode: null,
                    started.Elapsed);

                states.Add(state);
                LogModuleResult(target, state, command, traceId);
            }
            catch (Exception ex)
            {
                var state = new ModuleMigrationState(
                    descriptor.Module,
                    descriptor.Schema,
                    null,
                    [],
                    Succeeded: false,
                    ErrorCode: ex.GetType().Name,
                    started.Elapsed);
                states.Add(state);
                LogModuleResult(target, state, command, traceId);
                _logger.LogError(
                    ex,
                    "Migration {Command} failed. Edition={Edition} TenantId={TenantId} Database={Database} Module={Module} TraceId={TraceId}",
                    command,
                    target.Edition,
                    target.TenantId ?? string.Empty,
                    target.DatabaseLogicalName,
                    descriptor.Module,
                    traceId);
                break;
            }
        }

        return states;
    }

    private void LogModuleResult(
        MigrationTarget target,
        ModuleMigrationState state,
        MigrationRunnerCommand command,
        string traceId)
    {
        _logger.LogInformation(
            "Migration {Command} module={Module} edition={Edition} tenantId={TenantId} database={Database} connectionRef={ConnectionReference} schema={Schema} current={CurrentMigration} pendingCount={PendingCount} durationMs={DurationMs} result={Result} traceId={TraceId}",
            command,
            state.Module,
            target.Edition,
            target.TenantId ?? string.Empty,
            target.DatabaseLogicalName,
            target.ConnectionReference,
            state.Schema,
            state.CurrentMigration ?? "(none)",
            state.PendingMigrations.Count,
            (int)state.Duration.TotalMilliseconds,
            state.Succeeded ? "ok" : "failed",
            traceId);
    }
}

internal enum MigrationRunnerCommand
{
    Status,
    Plan,
    Apply,
}
