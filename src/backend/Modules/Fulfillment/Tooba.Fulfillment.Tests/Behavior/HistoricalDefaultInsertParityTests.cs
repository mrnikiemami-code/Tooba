using System.Data.Common;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Xunit;
using Xunit.Abstractions;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>
/// TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R12 — describing the five existing historical server defaults
/// must NOT change how EF writes those properties.
/// </summary>
/// <remarks>
/// <para>
/// The comparison is a real EF Core INSERT through the actual <c>FulfillmentDbContext</c> and the real Npgsql
/// provider against the disposable historical target: the captured statement's column list, the absence of a
/// <c>DEFAULT</c> keyword, the submitted parameters and the value read back from a NEW context must be identical
/// before and after the mapping change, for zero/empty values and for ordinary non-default values.
/// </para>
/// <para>
/// Entities are produced by the private parameterless constructor plus EF <c>CurrentValue</c> assignment, so this
/// is an ORM-contract boundary proof, not accepted domain input and not a production bypass.
/// </para>
/// <para>
/// <c>TB196R12_MODE=capture</c> records the baseline once; <c>verify</c> compares against it.
/// </para>
/// </remarks>
public sealed class HistoricalDefaultInsertParityTests(ITestOutputHelper output)
{
    private const string PrimaryDatabase = "fulfillment_tb196";

    private static readonly (string Entity, string Property, string Column, object Zero, object NonDefault)[] Cases =
    [
        ("items", "QuantityPacked", "quantity_packed", 0m, 2.5m),
        ("items", "QuantityProcessing", "quantity_processing", 0m, 4.25m),
        ("shipments", "ProviderMetadataVersion", "provider_metadata_version", 0, 7),
        ("shipments", "ShippingMethodCode", "shipping_method_code", string.Empty, "post:express"),
        ("shipments", "ShippingMethodLabel", "shipping_method_label", string.Empty, "Express"),
    ];

    [Fact]
    public async Task Ef_insert_semantics_for_the_five_historical_defaults_are_unchanged()
    {
        var target = RequireTarget();
        var mode = Environment.GetEnvironmentVariable("TB196R12_MODE") ?? "verify";
        var evidence = Environment.GetEnvironmentVariable("TB196R12_EVIDENCE_DIR");
        var lines = new List<string>();
        void Note(string line)
        {
            lines.Add(line);
            output.WriteLine(line);
        }

        await using (var context = Context(target, null))
        {
            await context.GetService<IMigrator>().MigrateAsync();
        }

        Note($"mapping {DescribeMapping(target)}");

        foreach (var (entity, property, column, zero, nonDefault) in Cases)
        {
            foreach (var (kind, value) in new[] { ("zero", zero), ("nondefault", nonDefault) })
            {
                var interceptor = new InsertCapture();
                var id = Guid.NewGuid();
                await using (var context = Context(target, interceptor))
                {
                    context.Add(NewMappedInstance(context, entity, id, property, value));
                    await context.SaveChangesAsync();
                }

                Assert.Single(interceptor.Statements);
                var statement = interceptor.Statements[0];
                Assert.StartsWith($"INSERT INTO fulfillment.{entity} (", statement, StringComparison.Ordinal);
                Assert.DoesNotContain("DEFAULT", statement, StringComparison.OrdinalIgnoreCase);
                var columnList = statement[(statement.IndexOf('(') + 1)..statement.IndexOf(')')];
                Assert.Contains(column, columnList, StringComparison.Ordinal);

                var submitted = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
                Assert.Contains(interceptor.Values, x => (Convert.ToString(x, CultureInfo.InvariantCulture) ?? string.Empty) == submitted);

                var readBack = await ReadScalarAsync(target, entity, id, column);
                Assert.Equal(entity == "items" ? decimal.Parse(submitted, CultureInfo.InvariantCulture).ToString("0.000000", CultureInfo.InvariantCulture) : submitted, readBack);
                Note($"case={entity}.{column}:{kind} explicit-column=True default-keyword=False readback={readBack}");
            }
        }

        // a required string given an explicit NULL must never be silently replaced by the server empty default
        var nullId = Guid.NewGuid();
        string? nullOutcome;
        await using (var nullContext = Context(target, null))
        {
            nullContext.Add(NewMappedInstance(nullContext, "shipments", nullId, "ShippingMethodCode", null));
            var failure = await Record.ExceptionAsync(() => nullContext.SaveChangesAsync());
            Assert.NotNull(failure);
            var postgres = FindPostgres(failure!);
            nullOutcome = postgres?.SqlState ?? failure!.GetType().Name;
            Assert.True(
                postgres is null || postgres.SqlState == "23502",
                $"unexpected provider outcome for explicit NULL: {postgres?.SqlState}");
        }

        Assert.Empty(await QueryAsync(
            target,
            $"select shipment_id::text from fulfillment.shipments where shipment_id = '{nullId}'"));
        Note($"explicit-null: {nullOutcome} rejected, no row substituted");

        // raw omit-column writers still receive the historical server defaults
        var rawShipment = Guid.NewGuid();
        await ExecuteAsync(target, $"""
            insert into fulfillment.shipments
                (shipment_id, fulfillment_id, carrier_display_name, created_at, status)
            values ('{rawShipment}', '{Guid.NewGuid()}', 'raw', now(), 'Pending');
            """);
        var rawRow = (await QueryAsync(
            target,
            "select provider_metadata_version::text, shipping_method_code, shipping_method_label "
            + $"from fulfillment.shipments where shipment_id = '{rawShipment}'")).Single().Split('|');
        Assert.Equal("0", rawRow[0]);
        Assert.Equal(string.Empty, rawRow[1]);
        Assert.Equal(string.Empty, rawRow[2]);

        var rawItem = Guid.NewGuid();
        await ExecuteAsync(target, $"""
            insert into fulfillment.items
                (fulfillment_item_id, fulfillment_id, order_line_id, quantity_ordered, quantity_shipped,
                 reservation_consumed)
            values ('{rawItem}', '{Guid.NewGuid()}', '{Guid.NewGuid()}', 1, 0, false);
            """);
        var rawQuantities = (await QueryAsync(
            target,
            "select quantity_packed::text, quantity_processing::text "
            + $"from fulfillment.items where fulfillment_item_id = '{rawItem}'")).Single().Split('|');
        Assert.Equal("0.000000", rawQuantities[0]);
        Assert.Equal("0.000000", rawQuantities[1]);
        Note($"raw-omit-column: version={rawRow[0]} code='{rawRow[1]}' label='{rawRow[2]}' packed={rawQuantities[0]} processing={rawQuantities[1]}");

        var rawNull = await TryRawAsync(target, $"""
            insert into fulfillment.shipments
                (shipment_id, fulfillment_id, carrier_display_name, created_at, status, shipping_method_code)
            values ('{Guid.NewGuid()}', '{Guid.NewGuid()}', 'raw', now(), 'Pending', NULL);
            """);
        Assert.Equal("23502", rawNull);
        Note($"raw-explicit-null: {rawNull} rejected");

        if (!string.IsNullOrWhiteSpace(evidence))
        {
            Directory.CreateDirectory(evidence);
            File.WriteAllText(
                Path.Combine(evidence, mode == "capture" ? "insert-parity-baseline.txt" : "insert-parity-after.txt"),
                string.Join(Environment.NewLine, lines) + Environment.NewLine);

            if (mode == "verify")
            {
                var baselinePath = Path.Combine(evidence, "insert-parity-baseline.txt");
                Assert.True(File.Exists(baselinePath), "the captured baseline is required for the parity comparison");
                var normalized = lines.Where(x => x.StartsWith("case=", StringComparison.Ordinal));
                var baseline = File.ReadAllLines(baselinePath)
                    .Where(x => x.StartsWith("case=", StringComparison.Ordinal));
                Assert.Equal(baseline, normalized);

                // the annotations are metadata-only: value generation and save behavior stay explicit
                using var context = Context(target, null);
                foreach (var propertyName in new[] { "QuantityPacked", "QuantityProcessing" })
                {
                    AssertExplicit(context.Model.FindEntityType(typeof(FulfillmentItem))!.FindProperty(propertyName)!);
                }

                foreach (var propertyName in new[] { "ProviderMetadataVersion", "ShippingMethodCode", "ShippingMethodLabel" })
                {
                    AssertExplicit(context.Model.FindEntityType(typeof(Shipment))!.FindProperty(propertyName)!);
                }
            }
        }
    }

    // ---------------------------------------------------------------------------------------------

    private static object NewMappedInstance(
        DbContext context,
        string entity,
        Guid id,
        string testedProperty,
        object? testedValue)
    {
        var type = entity == "items" ? typeof(FulfillmentItem) : typeof(Shipment);
        var instance = Activator.CreateInstance(type, nonPublic: true)
            ?? throw new InvalidOperationException("mapped entity requires its non-public constructor");
        var entry = context.Entry(instance);
        var now = new DateTimeOffset(2026, 10, 22, 12, 0, 0, TimeSpan.Zero);
        foreach (var property in entry.Metadata.GetProperties())
        {
            if (property.Name == testedProperty)
            {
                entry.Property(property.Name).CurrentValue = testedValue;
                continue;
            }

            if (property.IsNullable && !property.IsPrimaryKey())
            {
                continue;
            }

            entry.Property(property.Name).CurrentValue = property.Name switch
            {
                "FulfillmentItemId" or "ShipmentId" => id,
                "FulfillmentId" or "OrderLineId" => Guid.NewGuid(),
                "QuantityOrdered" or "QuantityShipped" => 1m,
                "CreatedAt" or "UpdatedAt" => now,
                _ => DefaultValue(property.ClrType),
            };
        }

        return instance;
    }

    private static object DefaultValue(Type type) =>
        type == typeof(string)
            ? string.Empty
            : type.IsEnum ? Enum.GetValues(type).GetValue(0)! : Activator.CreateInstance(type)!;

    private static string DescribeMapping(NpgsqlConnectionStringBuilder target)
    {
        using var context = Context(target, null);
        var descriptions = new List<string>();
        foreach (var propertyName in new[] { "QuantityPacked", "QuantityProcessing" })
        {
            descriptions.Add(Describe(context.Model.FindEntityType(typeof(FulfillmentItem))!.FindProperty(propertyName)!));
        }

        foreach (var propertyName in new[] { "ProviderMetadataVersion", "ShippingMethodCode", "ShippingMethodLabel" })
        {
            descriptions.Add(Describe(context.Model.FindEntityType(typeof(Shipment))!.FindProperty(propertyName)!));
        }

        return string.Join(" ", descriptions.Order(StringComparer.Ordinal));
    }

    private static string Describe(IProperty property) =>
        $"{property.Name}:generated={property.ValueGenerated},before={property.GetBeforeSaveBehavior()},after={property.GetAfterSaveBehavior()},default={property.GetDefaultValue() ?? "<none>"}";

    private static void AssertExplicit(IProperty property)
    {
        Assert.Equal(ValueGenerated.Never, property.ValueGenerated);
        Assert.Equal(PropertySaveBehavior.Save, property.GetBeforeSaveBehavior());
        Assert.Equal(PropertySaveBehavior.Save, property.GetAfterSaveBehavior());
    }

    private static async Task<string> ReadScalarAsync(
        NpgsqlConnectionStringBuilder target,
        string table,
        Guid id,
        string column)
    {
        var idColumn = table == "items" ? "fulfillment_item_id" : "shipment_id";
        var values = await QueryAsync(
            target,
            $"select coalesce({column}::text, '<null>') from fulfillment.{table} where {idColumn} = '{id}'");
        return values[0];
    }

    private static async Task<List<string>> QueryAsync(NpgsqlConnectionStringBuilder target, string sql)
    {
        var values = new List<string>();
        await using var connection = new NpgsqlConnection(target.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var fields = new List<string>();
            for (var index = 0; index < reader.FieldCount; index++)
            {
                fields.Add(reader.IsDBNull(index)
                    ? "<null>"
                    : Convert.ToString(reader.GetValue(index), CultureInfo.InvariantCulture) ?? "<null>");
            }

            values.Add(string.Join("|", fields));
        }

        return values;
    }

    private static async Task ExecuteAsync(NpgsqlConnectionStringBuilder target, string sql)
    {
        await using var connection = new NpgsqlConnection(target.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<string?> TryRawAsync(NpgsqlConnectionStringBuilder target, string sql)
    {
        try
        {
            await ExecuteAsync(target, sql);
            return null;
        }
        catch (PostgresException exception)
        {
            return exception.SqlState;
        }
    }

    private static PostgresException? FindPostgres(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgres)
            {
                return postgres;
            }
        }

        return null;
    }

    private static NpgsqlConnectionStringBuilder RequireTarget() =>
        TypeCatalogPostgresSchemaFixture.ValidateTarget(
            Environment.GetEnvironmentVariable("TB196R12_CONNECTION") ?? throw new InvalidOperationException(
                "TB196R12_CONNECTION is required: the insert-parity proof fails closed without its disposable target."),
            PrimaryDatabase);

    private static FulfillmentDbContext Context(
        NpgsqlConnectionStringBuilder target,
        DbCommandInterceptor? interceptor)
    {
        var options = new DbContextOptionsBuilder<FulfillmentDbContext>();
        Tooba.Persistence.ToobaNpgsql.ConfigureModuleContext(
            options,
            target.ConnectionString,
            FulfillmentDbContext.Schema,
            typeof(FulfillmentDbContext));
        if (interceptor is not null)
        {
            options.AddInterceptors(interceptor);
        }

        return new FulfillmentDbContext(options.Options);
    }

    private sealed class InsertCapture : DbCommandInterceptor
    {
        public List<string> Statements { get; } = [];

        public List<object?> Values { get; } = [];

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            Capture(command);
            return base.ReaderExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Capture(command);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        private void Capture(DbCommand command)
        {
            if (!command.CommandText.StartsWith("INSERT INTO fulfillment.", StringComparison.Ordinal))
            {
                return;
            }

            Statements.Add(command.CommandText.ReplaceLineEndings(" "));
            Values.Clear();
            foreach (DbParameter parameter in command.Parameters)
            {
                Values.Add(parameter.Value is DBNull ? null : parameter.Value);
            }
        }
    }
}
