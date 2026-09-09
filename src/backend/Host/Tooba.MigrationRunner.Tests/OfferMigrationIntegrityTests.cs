using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;
using Tooba.Offer.Domain;
using Tooba.Offer.Infrastructure.Persistence;
using Tooba.Persistence;
using Xunit;

namespace Tooba.MigrationRunner.Tests;

/// <summary>TB-P09-T013-R1: Offer history replay + T006/T013 column coexistence.</summary>
[Collection("MigrationRunnerSerial")]
public sealed class OfferMigrationIntegrityTests : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private bool _dockerAvailable;

    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("tooba_offer_integrity")
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
    public async Task Fresh_database_migrates_offer_to_head_without_collision()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");
        var cs = BuildConnection(_container!.GetConnectionString(), "offer_fresh");
        await CreateDatabaseAsync(_container.GetConnectionString(), "offer_fresh");

        await using var db = CreateOffer(cs);
        await db.Database.MigrateAsync();
        var applied = (await db.Database.GetAppliedMigrationsAsync()).ToList();
        Assert.Contains("20260823082919_InitialOffer", applied);
        Assert.Contains("20260907120000_AddOfferReturnPolicy", applied);
        Assert.Contains("20260909130100_AddOfferQuantityLimits", applied);
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        await AssertOfferColumnsAsync(cs);
    }

    [SkippableFact]
    public async Task Existing_db_with_return_policy_columns_and_missing_history_migrates_to_head()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");
        var cs = BuildConnection(_container!.GetConnectionString(), "offer_gap");
        await CreateDatabaseAsync(_container.GetConnectionString(), "offer_gap");

        await using var db = CreateOffer(cs);
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20260823082919_InitialOffer");

        await using (var conn = new NpgsqlConnection(cs))
        {
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(
                """
                ALTER TABLE offer.offers
                    ADD COLUMN return_policy_choice character varying(32) NOT NULL DEFAULT 'Default';
                ALTER TABLE offer.offers
                    ADD COLUMN custom_return_window_days integer NULL;
                """,
                conn);
            await cmd.ExecuteNonQueryAsync();
        }

        var afterManual = (await db.Database.GetAppliedMigrationsAsync()).ToList();
        Assert.Equal(["20260823082919_InitialOffer"], afterManual);

        await migrator.MigrateAsync();
        var applied = (await db.Database.GetAppliedMigrationsAsync()).ToList();
        Assert.Contains("20260907120000_AddOfferReturnPolicy", applied);
        Assert.Contains("20260909130100_AddOfferQuantityLimits", applied);
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        await AssertOfferColumnsAsync(cs);
    }

    [Fact]
    public void Offer_entity_has_exactly_one_return_policy_and_one_min_max_pair()
    {
        var names = typeof(SellerOffer).GetProperties().Select(p => p.Name).ToArray();
        Assert.Equal(1, names.Count(n => n == nameof(SellerOffer.ReturnPolicyChoice)));
        Assert.Equal(1, names.Count(n => n == nameof(SellerOffer.CustomReturnWindowDays)));
        Assert.Equal(1, names.Count(n => n == nameof(SellerOffer.MinimumOrderQuantity)));
        Assert.Equal(1, names.Count(n => n == nameof(SellerOffer.MaximumOrderQuantity)));
        Assert.DoesNotContain("UnitOfMeasureId", names);
        Assert.DoesNotContain("QuantityDecimalPlaces", names);
        Assert.DoesNotContain("QuantityStep", names);
    }

    private static OfferDbContext CreateOffer(string connectionString)
    {
        var options = new DbContextOptionsBuilder<OfferDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options,
            connectionString,
            OfferDbContext.Schema,
            typeof(OfferDbContext));
        return new OfferDbContext(options.Options);
    }

    private static string BuildConnection(string admin, string database) =>
        new NpgsqlConnectionStringBuilder(admin) { Database = database }.ConnectionString;

    private static async Task CreateDatabaseAsync(string adminConnection, string database)
    {
        await using var connection = new NpgsqlConnection(adminConnection);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"CREATE DATABASE \"{database}\"", connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task AssertOfferColumnsAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT column_name, data_type, numeric_precision, numeric_scale, is_nullable
            FROM information_schema.columns
            WHERE table_schema = 'offer' AND table_name = 'offers'
              AND column_name IN (
                'return_policy_choice',
                'custom_return_window_days',
                'minimum_order_quantity',
                'maximum_order_quantity')
            ORDER BY column_name;
            """,
            connection);
        await using var reader = await command.ExecuteReaderAsync();
        var rows = new Dictionary<string, (string Type, int? Precision, int? Scale, string Nullable)>(StringComparer.Ordinal);
        while (await reader.ReadAsync())
        {
            rows[reader.GetString(0)] = (
                reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetInt32(2),
                reader.IsDBNull(3) ? null : reader.GetInt32(3),
                reader.GetString(4));
        }

        Assert.Equal(4, rows.Count);
        Assert.Equal("character varying", rows["return_policy_choice"].Type);
        Assert.Equal("NO", rows["return_policy_choice"].Nullable);
        Assert.Equal("integer", rows["custom_return_window_days"].Type);
        Assert.Equal("YES", rows["custom_return_window_days"].Nullable);
        Assert.Equal("numeric", rows["minimum_order_quantity"].Type);
        Assert.Equal(18, rows["minimum_order_quantity"].Precision);
        Assert.Equal(6, rows["minimum_order_quantity"].Scale);
        Assert.Equal("YES", rows["minimum_order_quantity"].Nullable);
        Assert.Equal("numeric", rows["maximum_order_quantity"].Type);
        Assert.Equal(18, rows["maximum_order_quantity"].Precision);
        Assert.Equal(6, rows["maximum_order_quantity"].Scale);
        Assert.Equal("YES", rows["maximum_order_quantity"].Nullable);
    }
}
