using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Testcontainers.PostgreSql;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Host;
using Tooba.MigrationRunner;
using Xunit;

namespace Tooba.MigrationRunner.Tests;

[Collection("MigrationRunnerSerial")]
public sealed class MigrationRunnerIntegrationTests : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private bool _dockerAvailable;

    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("tooba_runner")
                .WithUsername("tooba")
                .WithPassword("dev-placeholder")
                .Build();
            await _container.StartAsync();
            _dockerAvailable = true;
        }
        catch (Exception)
        {
            _dockerAvailable = false;
        }
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    [SkippableFact]
    public async Task Apply_on_empty_database_succeeds_and_reapply_is_idempotent()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var connectionString = _container!.GetConnectionString();
        var options = BuildSingleStoreOptions(connectionString, "tenant-a", "db_a");
        var registry = PlatformOptionsValidator.BuildRegistry(options);
        var target = new MigrationTarget(
            ToobaEdition.SingleStore,
            "tenant-a",
            "store-a",
            "db_a");

        var orchestrator = CreateOrchestrator(options);
        var first = await orchestrator.RunAsync(target, MigrationRunnerCommand.Apply, CancellationToken.None);
        Assert.All(first, s => Assert.True(s.Succeeded));
        Assert.Contains(first, s => s.Module == "Catalog" && s.PendingMigrations.Count == 0);

        var second = await orchestrator.RunAsync(target, MigrationRunnerCommand.Apply, CancellationToken.None);
        Assert.All(second, s => Assert.True(s.Succeeded));
        Assert.All(second, s => Assert.Empty(s.PendingMigrations));
    }

    [SkippableFact]
    public async Task Plan_does_not_write_migration_history()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var connectionString = _container!.GetConnectionString();
        var options = BuildSingleStoreOptions(connectionString, "tenant-a", "db_plan");
        var target = new MigrationTarget(
            ToobaEdition.SingleStore,
            "tenant-a",
            "store-a",
            "db_plan");

        var orchestrator = CreateOrchestrator(options);
        var before = await ReadCatalogHistoryCountAsync(connectionString);
        var plan = await orchestrator.RunAsync(target, MigrationRunnerCommand.Plan, CancellationToken.None);
        var after = await ReadCatalogHistoryCountAsync(connectionString);

        Assert.Equal(before, after);
        Assert.Contains(plan, s => s.Module == "Catalog" && s.PendingMigrations.Count > 0);
    }

    [SkippableFact]
    public async Task Single_tenant_apply_does_not_touch_other_tenant_database()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var admin = _container!.GetConnectionString();
        await CreateDatabaseAsync(admin, "tenant_a_db");
        await CreateDatabaseAsync(admin, "tenant_b_db");

        var options = BuildTwoTenantOptions(admin, "tenant_a_db", "tenant_b_db");
        var orchestrator = CreateOrchestrator(options);

        var targetA = new MigrationTarget(
            ToobaEdition.SingleStore,
            "tenant-a",
            "store-a",
            "tenant_a_db");

        var applyA = await orchestrator.RunAsync(targetA, MigrationRunnerCommand.Apply, CancellationToken.None);
        Assert.All(applyA, s => Assert.True(s.Succeeded));

        var tenantBHistory = await ReadCatalogHistoryCountAsync(BuildConnection(admin, "tenant_b_db"));
        Assert.Equal(0, tenantBHistory);
    }

    [SkippableFact]
    public async Task Output_does_not_contain_password_literal()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var connectionString = _container!.GetConnectionString();
        var options = BuildSingleStoreOptions(connectionString, "tenant-a", "db_secret");
        var target = new MigrationTarget(
            ToobaEdition.SingleStore,
            "tenant-a",
            "store-a",
            "db_secret");

        var orchestrator = CreateOrchestrator(options);
        var states = await orchestrator.RunAsync(target, MigrationRunnerCommand.Status, CancellationToken.None);
        var serialized = string.Join('|', states.Select(s => s.Module));
        Assert.DoesNotContain("dev-placeholder", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("Password=", serialized, StringComparison.OrdinalIgnoreCase);
    }

    private static MigrationOrchestrator CreateOrchestrator(ToobaPlatformOptions options)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Options.Create(options));
        services.AddSingleton<DatabaseConnectionResolver>();
        services.AddSingleton<MigrationOrchestrator>();
        return services.BuildServiceProvider().GetRequiredService<MigrationOrchestrator>();
    }

    private static ToobaPlatformOptions BuildSingleStoreOptions(string connectionString, string reference, string database)
    {
        var cs = new Npgsql.NpgsqlConnectionStringBuilder(connectionString) { Database = database }.ConnectionString;
        return new ToobaPlatformOptions
        {
            Edition = "SingleStore",
            SingleStore = new SingleStoreOptions
            {
                Tenants =
                [
                    new TenantRecordOptions
                    {
                        TenantId = "store-a",
                        ConnectionReference = reference,
                        Hosts = ["localhost"],
                    },
                ],
            },
            PostgreSQL = new PostgreSqlOptions
            {
                ConnectionReferences = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [reference] = cs,
                },
            },
        };
    }

    private static ToobaPlatformOptions BuildTwoTenantOptions(string adminConnection, string dbA, string dbB)
    {
        return new ToobaPlatformOptions
        {
            Edition = "SingleStore",
            SingleStore = new SingleStoreOptions
            {
                Tenants =
                [
                    new TenantRecordOptions
                    {
                        TenantId = "store-a",
                        ConnectionReference = "tenant-a",
                        Hosts = ["a.localhost"],
                    },
                    new TenantRecordOptions
                    {
                        TenantId = "store-b",
                        ConnectionReference = "tenant-b",
                        Hosts = ["b.localhost"],
                    },
                ],
            },
            PostgreSQL = new PostgreSqlOptions
            {
                ConnectionReferences = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["tenant-a"] = BuildConnection(adminConnection, dbA),
                    ["tenant-b"] = BuildConnection(adminConnection, dbB),
                },
            },
        };
    }

    private static string BuildConnection(string admin, string database) =>
        new Npgsql.NpgsqlConnectionStringBuilder(admin) { Database = database }.ConnectionString;

    private static async Task CreateDatabaseAsync(string adminConnection, string database)
    {
        await using var connection = new Npgsql.NpgsqlConnection(adminConnection);
        await connection.OpenAsync();
        await using var command = new Npgsql.NpgsqlCommand($"CREATE DATABASE \"{database}\"", connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<int> ReadCatalogHistoryCountAsync(string connectionString)
    {
        await using var connection = new Npgsql.NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new Npgsql.NpgsqlCommand(
            $"SELECT COUNT(*) FROM {CatalogDbContext.Schema}.__ef_migrations_history",
            connection);
        try
        {
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            return 0;
        }
    }
}

[CollectionDefinition("MigrationRunnerSerial", DisableParallelization = true)]
public sealed class MigrationRunnerSerialCollection;
