// TB-SHIPPING-ONE-LEVEL-TABS-083 — isolated PostgreSQL proof for the one-level Fulfillment catalog.
// Creates a disposable database, applies the real EF migration history, exercises constraints and the
// reader, then rolls back and drops the database. Never touches tooba_alpha or the running Host.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Fulfillment.Infrastructure.Shipping;
using Tooba.Persistence;

const string Admin = "Host=127.0.0.1;Port=5432;Username=admin;Password=123456;Database=postgres";
var dbName = "tooba_onelevel083_" + Guid.NewGuid().ToString("N")[..8];
var conn = $"Host=127.0.0.1;Port=5432;Username=admin;Password=123456;Database={dbName}";

var log = new List<string>();
void Say(string s) { log.Add(s); Console.WriteLine(s); }

NpgsqlConnection AdminConn() => new(Admin);

async Task<long> ScalarAsync(NpgsqlConnection c, string sql)
{
    await using var cmd = new NpgsqlCommand(sql, c);
    var v = await cmd.ExecuteScalarAsync();
    return v is null or DBNull ? -1 : Convert.ToInt64(v);
}

FulfillmentDbContext Db()
{
    var options = new DbContextOptionsBuilder<FulfillmentDbContext>();
    ToobaNpgsql.ConfigureModuleContext(options, conn, FulfillmentDbContext.Schema, typeof(FulfillmentDbContext));
    return new FulfillmentDbContext(options.Options);
}

try
{
    await using (var admin = AdminConn())
    {
        await admin.OpenAsync();
        await using var create = new NpgsqlCommand($"CREATE DATABASE \"{dbName}\"", admin);
        await create.ExecuteNonQueryAsync();
    }
    Say($"ISOLATED_DB_CREATED {dbName}");

    // ---- positive provenance: the target is a brand-new database, not tooba_alpha -----------------
    await using (var c = new NpgsqlConnection(conn))
    {
        await c.OpenAsync();
        var current = await ScalarAsync(c, "select count(*) from pg_namespace where nspname in ('fulfillment','shipping','order','cart')");
        var alphaTables = await ScalarAsync(c, "select count(*) from pg_class where relname = 'shipping_services'");
        Say($"PROVENANCE current_db={await ScalarAsync(c, "select 1")} foreign_schemas={current} preexisting_shipping_services={alphaTables}");
    }

    // ---- UP: apply the real migration history (includes 20261015120000_AddShippingOneLevelCatalog) --
    await using (var db = Db())
    {
        var pending = (await db.Database.GetPendingMigrationsAsync()).ToArray();
        Say($"PENDING_MIGRATIONS {pending.Length} :: {string.Join(",", pending)}");
        await db.Database.MigrateAsync();
    }

    await using (var c = new NpgsqlConnection(conn))
    {
        await c.OpenAsync();
        var applied = await ScalarAsync(c, "select count(*) from fulfillment.__ef_migrations_history");
        var current = await ScalarAsync(c, "select count(*) from fulfillment.__ef_migrations_history where migration_id like '%AddShippingOneLevelCatalog'");
        var cols = await ScalarAsync(c, @"select count(*) from information_schema.columns
            where table_schema='fulfillment' and table_name='shipping_methods'
              and column_name in ('shipping_method_id','method_code','provider_kind','legacy_service_code','legacy_option_code','sort_order','is_active','created_at','updated_at')");
        var codeWidth = await ScalarAsync(c, @"select character_maximum_length from information_schema.columns
            where table_schema='fulfillment' and table_name='shipping_methods' and column_name='method_code'");
        var idx = await ScalarAsync(c, @"select count(*) from pg_indexes where schemaname='fulfillment'
            and indexname in ('ix_shipping_methods_method_code','ux_shipping_methods_legacy_binding','ix_shipping_methods_legacy_service_code','ix_shipping_method_translations_method_lang')");
        var checks = await ScalarAsync(c, @"select count(*) from pg_constraint where contype='c'
            and conname in ('ck_shipping_methods_code_canonical','ck_shipping_methods_option_code_canonical')");
        var legacyTables = await ScalarAsync(c, @"select count(*) from information_schema.tables
            where table_schema='fulfillment' and table_name in ('shipping_services','shipping_service_options','shipping_service_translations','shipping_service_option_translations')");
        Say($"UP history_total={applied} onelevel_applied={current} columns={cols} method_code_width={codeWidth} indexes={idx} checks={checks} legacy_catalog_tables={legacyTables}");
    }

    // ---- seed representative methods + translations ------------------------------------------------
    var postId = Guid.NewGuid();
    var tipaxStdId = Guid.NewGuid();
    var storeId = Guid.NewGuid();
    var snappId = Guid.NewGuid();
    var langFa = Guid.Parse("01900000-0000-7000-8000-00000000aa01");
    var now = DateTimeOffset.UtcNow;

    await using (var db = Db())
    {
        db.ShippingMethods.Add(ShippingMethod.Create(postId, "post:standard", "post", "post", "standard", 12, true, now));
        db.ShippingMethods.Add(ShippingMethod.Create(tipaxStdId, "tipax:standard", "tipax", "tipax", "standard", 21, true, now));
        db.ShippingMethods.Add(ShippingMethod.Create(storeId, "store_courier", "store_courier", "store_courier", null, 40, true, now));
        db.ShippingMethods.Add(ShippingMethod.Create(snappId, "snapp_courier", "courier", "snapp_courier", null, 30, false, now));
        db.ShippingMethodTranslations.Add(ShippingMethodTranslation.Create(Guid.NewGuid(), postId, langFa, "پست معمولی", null));
        await db.SaveChangesAsync();
    }
    Say("INSERT representative_methods=4 translations=1 OK");

    // ---- constraint negative cases ------------------------------------------------------------------
    var negatives = new List<string>();
    async Task ExpectReject(string label, string sql)
    {
        await using var c = new NpgsqlConnection(conn);
        await c.OpenAsync();
        try
        {
            await using var cmd = new NpgsqlCommand(sql, c);
            await cmd.ExecuteNonQueryAsync();
            negatives.Add($"NOT_REJECTED:{label}");
        }
        catch (PostgresException ex)
        {
            negatives.Add($"{label} -> {ex.SqlState}/{ex.ConstraintName}");
        }
    }

    var baseCols = "(shipping_method_id, method_code, provider_kind, legacy_service_code, legacy_option_code, sort_order, is_active, created_at, updated_at)";
    await ExpectReject("duplicate_method_code",
        $"insert into fulfillment.shipping_methods {baseCols} values (gen_random_uuid(),'post:standard','post','post','standard',1,true,now(),now())");
    await ExpectReject("duplicate_legacy_binding",
        $"insert into fulfillment.shipping_methods {baseCols} values (gen_random_uuid(),'post:other','post','post','standard',1,true,now(),now())");
    await ExpectReject("over_32_chars",
        $"insert into fulfillment.shipping_methods {baseCols} values (gen_random_uuid(),'{new string('a', 33)}','post','post','x9',1,true,now(),now())");
    await ExpectReject("uppercase_code",
        $"insert into fulfillment.shipping_methods {baseCols} values (gen_random_uuid(),'POST:UPPER','post','post','x8',1,true,now(),now())");
    await ExpectReject("hyphen_separator",
        $"insert into fulfillment.shipping_methods {baseCols} values (gen_random_uuid(),'post-standard','post','post','x7',1,true,now(),now())");
    await ExpectReject("blank_option_code",
        $"insert into fulfillment.shipping_methods {baseCols} values (gen_random_uuid(),'post:blankopt','post','post','  ',1,true,now(),now())");
    await ExpectReject("duplicate_translation_language",
        $"insert into fulfillment.shipping_method_translations (translation_id, shipping_method_id, language_id, name) values (gen_random_uuid(),'{postId}','{langFa}','dup')");
    await ExpectReject("fk_missing_method_translation",
        $"insert into fulfillment.shipping_method_translations (translation_id, shipping_method_id, language_id, name) values (gen_random_uuid(),'{Guid.NewGuid()}','{langFa}','orphan')");
    foreach (var n in negatives) Say($"NEGATIVE {n}");
    var rejected = negatives.Count(x => !x.StartsWith("NOT_REJECTED", StringComparison.Ordinal));
    Say($"NEGATIVE_SUMMARY rejected={rejected}/{negatives.Count}");

    // ---- two optionless methods may coexist (partial unique index must not collide) ------------------
    var noCollision = "NO_COLLISION_PROBE_NOT_RUN";
    try
    {
        await using var db = Db();
        db.ShippingMethods.Add(ShippingMethod.Create(Guid.NewGuid(), "in_person", "in_person", "in_person", null, 50, true, now));
        await db.SaveChangesAsync();
        noCollision = "OPTIONLESS_ROWS_COEXIST_OK";
    }
    catch (Exception ex) { noCollision = "OPTIONLESS_COLLISION:" + ex.GetType().Name; }
    Say(noCollision);

    // ---- read through the real reader ---------------------------------------------------------------
    await using (var db = Db())
    {
        var reader = new ShippingOneLevelCatalogReader(db);
        var all = await reader.ListAsync(CancellationToken.None);
        var active = await reader.ListActiveAsync(CancellationToken.None);
        var one = await reader.GetByCodeAsync("POST:Standard", CancellationToken.None);
        var missing = await reader.GetByCodeAsync("post:does-not-exist", CancellationToken.None);
        var bad = await reader.GetByCodeAsync("post standard", CancellationToken.None);
        Say($"READER all={all.Count} active={active.Count} get_case_insensitive={(one?.MethodCode ?? "null")} " +
            $"get_missing={(missing is null ? "null" : "unexpected")} get_malformed={(bad is null ? "null" : "unexpected")} " +
            $"post_translation={(one?.Translations.FirstOrDefault()?.Name ?? "none")} " +
            $"optionless_binding={(all.First(x => x.MethodCode == "store_courier").LegacyOptionCode is null ? "null" : "set")} " +
            $"inactive_present={(all.Any(x => x.MethodCode == "snapp_courier" && !x.IsActive) ? "yes" : "no")}");
    }

    // ---- 32-char persisted identity boundary --------------------------------------------------------
    var exact32 = new string('a', 32);
    await using (var db = Db())
    {
        db.ShippingMethods.Add(ShippingMethod.Create(Guid.NewGuid(), exact32, "post", "post", "at32", 60, true, now));
        await db.SaveChangesAsync();
    }
    await using (var db = Db())
    {
        var reader = new ShippingOneLevelCatalogReader(db);
        var found = await reader.GetByCodeAsync(exact32, CancellationToken.None);
        Say($"BOUNDARY persisted_32_chars_readback={(found?.MethodCode.Length.ToString() ?? "null")}");
    }
    await using (var c = new NpgsqlConnection(conn))
    {
        await c.OpenAsync();
        var len = await ScalarAsync(c, "select max(length(method_code)) from fulfillment.shipping_methods");
        Say($"BOUNDARY max_persisted_code_length={len}");
    }

    // ---- legacy catalog untouched -------------------------------------------------------------------
    await using (var c = new NpgsqlConnection(conn))
    {
        await c.OpenAsync();
        var services = await ScalarAsync(c, "select count(*) from fulfillment.shipping_services");
        var options = await ScalarAsync(c, "select count(*) from fulfillment.shipping_service_options");
        var methods = await ScalarAsync(c, "select count(*) from fulfillment.shipping_methods");
        Say($"LEGACY_UNTOUCHED services={services} options={options} new_methods={methods}");
    }

    // ---- DOWN: revert only the new migration --------------------------------------------------------
    await using (var db = Db())
    {
        var migrator = db.Database.GetInfrastructure().GetRequiredService<IMigrator>();
        await migrator.MigrateAsync("20260910170000_AddShippingServiceCatalog");
    }
    await using (var c = new NpgsqlConnection(conn))
    {
        await c.OpenAsync();
        var methodsTable = await ScalarAsync(c, @"select count(*) from information_schema.tables
            where table_schema='fulfillment' and table_name in ('shipping_methods','shipping_method_translations')");
        var legacyAfter = await ScalarAsync(c, @"select count(*) from information_schema.tables
            where table_schema='fulfillment' and table_name in ('shipping_services','shipping_service_options')");
        var history = await ScalarAsync(c, "select count(*) from fulfillment.__ef_migrations_history where migration_id like '%AddShippingOneLevelCatalog'");
        Say($"DOWN new_tables_remaining={methodsTable} legacy_tables_remaining={legacyAfter} onelevel_history_rows={history}");
    }

    // ---- re-UP after DOWN (proves reversibility is not one-way) -------------------------------------
    await using (var db = Db())
    {
        await db.Database.MigrateAsync();
    }
    await using (var c = new NpgsqlConnection(conn))
    {
        await c.OpenAsync();
        var back = await ScalarAsync(c, @"select count(*) from information_schema.tables
            where table_schema='fulfillment' and table_name in ('shipping_methods','shipping_method_translations')");
        Say($"REUP new_tables_after_reapply={back}");
    }
}
catch (Exception ex)
{
    Say("FAILED " + ex);
}
finally
{
    try
    {
        await using var admin = AdminConn();
        await admin.OpenAsync();
        await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{dbName}\" WITH (FORCE)", admin);
        await drop.ExecuteNonQueryAsync();
        Say($"ISOLATED_DB_DROPPED {dbName}");
    }
    catch (Exception ex) { Say("DROP_FAILED " + ex.Message); }

    try
    {
        await using var admin = AdminConn();
        await admin.OpenAsync();
        var leftover = await ScalarAsync(admin, "select count(*) from pg_database where datname like 'tooba_onelevel083_%'");
        Say($"LEFTOVER_ISOLATED_DATABASES {leftover}");
    }
    catch (Exception ex) { Say("LEFTOVER_CHECK_FAILED " + ex.Message); }
}

File.WriteAllLines(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "log-isolated-083.txt"), log);
