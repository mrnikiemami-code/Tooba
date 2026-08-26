using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;
using Tooba.Host;

namespace Tooba.MigrationRunner;

/// <summary>
/// نقطهٔ ورود CLI مهاجرت تولید.
/// </summary>
internal static class Program
{
    private static readonly TimeSpan LockTimeout = TimeSpan.FromMinutes(2);

    public static async Task<int> Main(string[] args)
    {
        if (!MigrationRunnerCli.TryParse(args, out var cli, out var parseError))
        {
            Console.Error.WriteLine(parseError);
            return 3;
        }

        using var activity = new Activity("Tooba.MigrationRunner").Start();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.AddJsonConsole(options =>
            {
                options.IncludeScopes = true;
                options.TimestampFormat = "O";
                options.UseUtcTimestamp = true;
            });
            builder.SetMinimumLevel(LogLevel.Information);
        });

        services.Configure<ToobaPlatformOptions>(configuration.GetSection(ToobaPlatformOptions.SectionName));
        services.AddSingleton<IValidateOptions<ToobaPlatformOptions>, PlatformOptionsValidator>();
        services.AddSingleton(sp => PlatformOptionsValidator.BuildRegistry(sp.GetRequiredService<IOptions<ToobaPlatformOptions>>().Value));
        services.AddSingleton<DatabaseConnectionResolver>();
        services.AddSingleton<MigrationOrchestrator>();

        await using var provider = services.BuildServiceProvider();
        var platformOptions = provider.GetRequiredService<IOptions<ToobaPlatformOptions>>().Value;
        var registry = provider.GetRequiredService<ControlPlaneRegistry>();
        var orchestrator = provider.GetRequiredService<MigrationOrchestrator>();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Tooba.MigrationRunner");

        IReadOnlyList<MigrationTarget> targets;
        try
        {
            targets = MigrationTargetResolver.Resolve(registry, platformOptions, cli);
        }
        catch (MigrationRunnerException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return ex.ExitCode;
        }

        var anyFailure = false;
        foreach (var target in targets)
        {
            var connectionString = provider.GetRequiredService<DatabaseConnectionResolver>()
                .Resolve(new ConnectionReference(target.ConnectionReference));

            if (cli.Command == MigrationRunnerCommand.Apply)
            {
                var migrationLock = await PostgresMigrationAdvisoryLock.TryAcquireAsync(
                    connectionString,
                    $"{target.ConnectionReference}:{target.DatabaseLogicalName}",
                    LockTimeout,
                    CancellationToken.None);

                if (migrationLock is null)
                {
                    Console.Error.WriteLine(
                        $"Could not acquire migration lock for database '{target.DatabaseLogicalName}' within {LockTimeout.TotalSeconds:n0}s.");
                    return 2;
                }

                await using (migrationLock)
                {
                    anyFailure |= !await ExecuteTargetAsync(orchestrator, target, cli.Command, logger);
                }
            }
            else
            {
                anyFailure |= !await ExecuteTargetAsync(orchestrator, target, cli.Command, logger);
            }
        }

        return anyFailure ? 1 : 0;
    }

    private static async Task<bool> ExecuteTargetAsync(
        MigrationOrchestrator orchestrator,
        MigrationTarget target,
        MigrationRunnerCommand command,
        ILogger logger)
    {
        var states = await orchestrator.RunAsync(target, command, CancellationToken.None);
        var payload = new
        {
            command = command.ToString(),
            edition = target.Edition.ToString(),
            tenantId = target.TenantId,
            database = target.DatabaseLogicalName,
            connectionReference = target.ConnectionReference,
            modules = states.Select(s => new
            {
                module = s.Module,
                schema = s.Schema,
                currentMigration = s.CurrentMigration,
                pendingMigrations = s.PendingMigrations,
                succeeded = s.Succeeded,
                durationMs = (int)s.Duration.TotalMilliseconds,
            }),
        };

        Console.WriteLine(JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));

        var failed = states.Any(s => !s.Succeeded);
        if (failed)
        {
            logger.LogError(
                "Migration target failed. Edition={Edition} TenantId={TenantId} Database={Database}",
                target.Edition,
                target.TenantId ?? string.Empty,
                target.DatabaseLogicalName);
        }

        return !failed;
    }
}
