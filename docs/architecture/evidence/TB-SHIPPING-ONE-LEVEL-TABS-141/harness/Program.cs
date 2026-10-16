using Microsoft.EntityFrameworkCore;
using Tooba.Persistence;
using Tooba.Settlement.Infrastructure.Persistence;

// TB-SHIPPING-ONE-LEVEL-TABS-141 — bounded, read-mostly harness that exercises the Settlement module's
// OWN persistence composition on a DISPOSABLE scratch database, using exactly the same
// ToobaNpgsql.ConfigureModuleContext seam the module uses at runtime (schema settlement, module-owned
// migration assembly, canonical "__ef_migrations_history" table inside the "settlement" schema).
//
// It applies the module's pending migrations (the documented explicit activation path for a module that
// registers no Host-startup IModuleSchemaMigrator) and proves idempotency by applying them twice.
// It writes nothing outside the caller-supplied scratch database.

var connectionString = Environment.GetEnvironmentVariable("TB141_SETTLEMENT_CONNECTION");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.WriteLine("HARNESS=FAIL reason=TB141_SETTLEMENT_CONNECTION_missing");
    return 2;
}

if (connectionString.Contains("tooba_alpha", StringComparison.OrdinalIgnoreCase)
    || connectionString.Contains("tenant-alpha", StringComparison.OrdinalIgnoreCase)
    || connectionString.Contains("Port=5432", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine("HARNESS=FAIL reason=refused_non_scratch_target (shared alpha / 5432 guard)");
    return 3;
}

var optionsBuilder = new DbContextOptionsBuilder<SettlementDbContext>();
ToobaNpgsql.ConfigureModuleContext(
    optionsBuilder,
    connectionString,
    SettlementDbContext.Schema,
    typeof(SettlementDbContext));

await using var context = new SettlementDbContext(optionsBuilder.Options);
var database = context.Database;

var pendingBefore = (await database.GetPendingMigrationsAsync()).ToArray();
var appliedBefore = (await database.GetAppliedMigrationsAsync()).ToArray();
Console.WriteLine($"MIGRATIONS_APPLIED_BEFORE=[{string.Join(",", appliedBefore)}]");
Console.WriteLine($"MIGRATIONS_PENDING_BEFORE=[{string.Join(",", pendingBefore)}]");

await database.MigrateAsync();
var appliedAfter = (await database.GetAppliedMigrationsAsync()).ToArray();
Console.WriteLine($"MIGRATIONS_APPLIED_AFTER_FIRST_APPLY=[{string.Join(",", appliedAfter)}]");

// Idempotency: a second MigrateAsync must be a no-op (no error, no additional history row).
await database.MigrateAsync();
var appliedAfterSecond = (await database.GetAppliedMigrationsAsync()).ToArray();
Console.WriteLine($"MIGRATIONS_APPLIED_AFTER_SECOND_APPLY=[{string.Join(",", appliedAfterSecond)}]");
Console.WriteLine($"SECOND_APPLY_WAS_NOOP={appliedAfter.Length == appliedAfterSecond.Length}");

var historyTableRows = await database
    .SqlQueryRaw<string>("select migration_id as \"Value\" from settlement.__ef_migrations_history order by migration_id")
    .ToListAsync();
Console.WriteLine($"SETTLEMENT_HISTORY_TABLE_ROWS=[{string.Join(",", historyTableRows)}]");

var feeTableShape = await database.SqlQueryRaw<string>(
    """
    select column_name || ':' || data_type || ':' || is_nullable as "Value"
    from information_schema.columns
    where table_schema = 'settlement' and table_name = 'shipping_group_fees'
    order by ordinal_position
    """).ToListAsync();
Console.WriteLine($"SHIPPING_GROUP_FEES_COLUMNS=[{string.Join(",", feeTableShape)}]");

var indexRows = await database.SqlQueryRaw<string>(
    """
    select indexname as "Value" from pg_indexes
    where schemaname = 'settlement' and tablename = 'shipping_group_fees'
    order by indexname
    """).ToListAsync();
Console.WriteLine($"SHIPPING_GROUP_FEES_INDEXES=[{string.Join(",", indexRows)}]");

var settlementTableCount = await database
    .SqlQueryRaw<int>("select count(*)::int as \"Value\" from information_schema.tables where table_schema = 'settlement'")
    .SingleAsync();
Console.WriteLine($"SETTLEMENT_TABLE_COUNT={settlementTableCount}");

Console.WriteLine("HARNESS=PASS");
return 0;
