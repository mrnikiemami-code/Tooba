// TB-SHIPPING-ONE-LEVEL-TABS-090 — real Development backfill proof on the positively identified
// tooba_alpha database. Proves: (1) a plan with violations is refused before any write, (2) the apply
// is all-or-nothing on real PostgreSQL, (3) the committed mapping matches the live legacy catalog,
// (4) a re-run is a no-op (every decision Skip), (5) the legacy two-level catalog is untouched.
// It never deletes or rewrites a legacy row, never touches checkout/order/payment data, and never
// enables any gate.
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application.Shipping;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Fulfillment.Infrastructure.Shipping;
using Tooba.Persistence;

const string Conn = "Host=127.0.0.1;Port=5432;Username=admin;Password=123456;Database=tooba_alpha";

var log = new List<string>();
void Say(string s) { log.Add(s); Console.WriteLine(s); }

FulfillmentDbContext Db()
{
    var options = new DbContextOptionsBuilder<FulfillmentDbContext>();
    ToobaNpgsql.ConfigureModuleContext(options, Conn, FulfillmentDbContext.Schema, typeof(FulfillmentDbContext));
    return new FulfillmentDbContext(options.Options);
}

async Task<long> ScalarAsync(string sql)
{
    await using var c = new NpgsqlConnection(Conn);
    await c.OpenAsync();
    await using var cmd = new NpgsqlCommand(sql, c);
    var v = await cmd.ExecuteScalarAsync();
    return v is null or DBNull ? -1 : Convert.ToInt64(v);
}

ShippingOneLevelBackfillStore Store(FulfillmentDbContext db) =>
    new(db, new ShippingCatalogReader(db), new SystemUtcClock(), new UuidV7IdGenerator());

try
{
    Say($"TARGET database={await ScalarAsync("select 1")} db_name={await ScalarAsync("select 1 from pg_database where datname='tooba_alpha'")}");

    // ---- pre-state --------------------------------------------------------------------------------
    Say($"PRE methods={await ScalarAsync("select count(*) from fulfillment.shipping_methods")} " +
        $"method_translations={await ScalarAsync("select count(*) from fulfillment.shipping_method_translations")} " +
        $"legacy_services={await ScalarAsync("select count(*) from fulfillment.shipping_services")} " +
        $"legacy_options={await ScalarAsync("select count(*) from fulfillment.shipping_service_options")}");

    // ---- guard: a violating plan is refused before any transaction/write --------------------------
    string guard;
    await using (var db = Db())
    {
        var store = Store(db);
        var bad = new ShippingMethodBackfillPlan(
            [new ShippingMethodPlanItem(
                ShippingMethodPlanAction.Create,
                ShippingMethodBackfillPlanner.Plan(await new ShippingCatalogReader(db).ListAsync(CancellationToken.None), [])
                    .Items[0].Desired,
                null)],
            ["binding_mismatch:post:standard"]);
        try
        {
            await store.ApplyAsync(bad, CancellationToken.None);
            guard = "NOT_REFUSED";
        }
        catch (InvalidOperationException ex) { guard = "REFUSED:" + ex.Message; }
    }
    Say($"GUARD violating_plan {guard} methods_after={await ScalarAsync("select count(*) from fulfillment.shipping_methods")}");

    // ---- atomicity on real PostgreSQL: a colliding create must roll back every earlier create ----
    string atomic;
    await using (var db = Db())
    {
        var store = Store(db);
        var plan = ShippingMethodBackfillPlanner.Plan(await new ShippingCatalogReader(db).ListAsync(CancellationToken.None), []);
        var first = plan.Items[0].Desired;
        // Two Create items with the same canonical code: the second insert must violate the unique
        // index on method_code and roll back the first one too. This is a guard test of the seam,
        // not a plan the planner can produce.
        var colliding = new ShippingMethodBackfillPlan(
            [new ShippingMethodPlanItem(ShippingMethodPlanAction.Create, first, null),
             new ShippingMethodPlanItem(ShippingMethodPlanAction.Create, first, null)],
            []);
        try
        {
            await store.ApplyAsync(colliding, CancellationToken.None);
            atomic = "NOT_ROLLED_BACK";
        }
        catch (Exception ex) { atomic = "ROLLED_BACK:" + ex.GetType().Name; }
    }
    Say($"ATOMICITY colliding_plan {atomic} methods_after={await ScalarAsync("select count(*) from fulfillment.shipping_methods")}");

    // ---- dry-run the real plan -------------------------------------------------------------------
    ShippingMethodBackfillPlan real;
    await using (var db = Db())
    {
        var store = Store(db);
        real = ShippingMethodBackfillPlanner.Plan(
            await new ShippingCatalogReader(db).ListAsync(CancellationToken.None),
            await store.ListExistingAsync(CancellationToken.None));
        Say($"DRYRUN executable={real.IsExecutable} items={real.Items.Count} violations={real.Violations.Count} " +
            $"creates={real.Items.Count(x => x.Action == ShippingMethodPlanAction.Create)} " +
            $"skips={real.Items.Count(x => x.Action == ShippingMethodPlanAction.Skip)} " +
            $"aligns={real.Items.Count(x => x.Action == ShippingMethodPlanAction.AlignActivation)}");
        foreach (var v in real.Violations) Say($"DRYRUN_VIOLATION {v}");
        foreach (var i in real.Items)
            Say($"DRYRUN_ITEM {i.Action} code={i.Desired.MethodCode} service={i.Desired.LegacyServiceCode} " +
                $"option={i.Desired.LegacyOptionCode ?? "null"} active={i.Desired.IsActive} sort={i.Desired.SortOrder} " +
                $"translations={i.Desired.Translations.Count}");

        // ---- commit the backfill -----------------------------------------------------------------
        await store.ApplyAsync(real, CancellationToken.None);
    }
    Say($"APPLIED methods={await ScalarAsync("select count(*) from fulfillment.shipping_methods")} " +
        $"method_translations={await ScalarAsync("select count(*) from fulfillment.shipping_method_translations")}");

    // ---- persisted mapping read-back ------------------------------------------------------------
    await using (var db = Db())
    {
        var store = Store(db);
        foreach (var row in (await store.ListExistingAsync(CancellationToken.None))
                     .OrderBy(x => x.MethodCode, StringComparer.Ordinal))
        {
            Say($"ROW code={row.MethodCode} service={row.LegacyServiceCode} option={row.LegacyOptionCode ?? "null"} active={row.IsActive}");
        }

        var second = ShippingMethodBackfillPlanner.Plan(
            await new ShippingCatalogReader(db).ListAsync(CancellationToken.None),
            await store.ListExistingAsync(CancellationToken.None));
        Say($"RERUN executable={second.IsExecutable} items={second.Items.Count} " +
            $"all_skip={second.Items.All(x => x.Action == ShippingMethodPlanAction.Skip)} violations={second.Violations.Count}");
        await store.ApplyAsync(second, CancellationToken.None);
    }
    Say($"RERUN_APPLIED methods={await ScalarAsync("select count(*) from fulfillment.shipping_methods")} " +
        $"method_translations={await ScalarAsync("select count(*) from fulfillment.shipping_method_translations")}");

    // ---- legacy catalog and historical codes untouched -------------------------------------------
    Say($"LEGACY_UNTOUCHED services={await ScalarAsync("select count(*) from fulfillment.shipping_services")} " +
        $"options={await ScalarAsync("select count(*) from fulfillment.shipping_service_options")} " +
        $"service_translations={await ScalarAsync("select count(*) from fulfillment.shipping_service_translations")} " +
        $"option_translations={await ScalarAsync("select count(*) from fulfillment.shipping_service_option_translations")}");
    Say($"HISTORICAL_UNTOUCHED fulfillments={await ScalarAsync("select count(*) from fulfillment.fulfillments")} " +
        $"shipments={await ScalarAsync("select count(*) from fulfillment.shipments")} " +
        $"drafts={await ScalarAsync("select count(*) from \"order\".cart_shipping_drafts")}");

    // ---- per-language names ----------------------------------------------------------------------
    await using (var db = Db())
    {
        foreach (var t in await db.ShippingMethodTranslations.AsNoTracking()
                     .Join(db.ShippingMethods.AsNoTracking(), t => t.ShippingMethodId, m => m.ShippingMethodId,
                         (t, m) => new { m.MethodCode, t.LanguageId, t.Name })
                     .OrderBy(x => x.MethodCode).ThenBy(x => x.LanguageId).ToListAsync())
        {
            Say($"TRANSLATION code={t.MethodCode} lang={t.LanguageId} name={t.Name}");
        }
    }

    // ---- a service-level row is never invented ---------------------------------------------------
    Say($"NO_SERVICE_LEVEL_ROW bare_post={await ScalarAsync("select count(*) from fulfillment.shipping_methods where method_code='post'")} " +
        $"bare_tipax={await ScalarAsync("select count(*) from fulfillment.shipping_methods where method_code='tipax'")}");
}
catch (Exception ex)
{
    Say("FAILED " + ex);
}

File.WriteAllLines(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "log-backfill-090.txt"), log);
