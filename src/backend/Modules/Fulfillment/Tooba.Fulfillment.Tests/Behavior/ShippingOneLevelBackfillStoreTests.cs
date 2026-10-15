using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application.Shipping;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Fulfillment.Infrastructure.Shipping;
using Xunit;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>
/// TB-SHIPPING-ONE-LEVEL-TABS-090 — focused proof for the persisted one-level backfill seam on the
/// decision side: the dry-run planner is the only decision maker, it reads the real persisted catalog
/// shape, it keeps each language's own name, and a violating plan is refused before any write.
/// The transactional apply / idempotency / rollback proof runs against a real isolated PostgreSQL
/// database in the task evidence (the in-memory store has no transactions, so it must not fake one).
/// </summary>
public sealed class ShippingOneLevelBackfillStoreTests
{
    private static readonly Guid LangFa = Guid.Parse("01900000-0000-7000-8000-00000000aa01");
    private static readonly Guid LangEn = Guid.Parse("01900000-0000-7000-8000-00000000aa02");

    [Fact]
    public async Task Dry_run_reads_the_persisted_catalog_and_plans_every_method_without_writing()
    {
        await using var db = CreateDb();
        SeedLegacyCatalog(db);
        var store = Store(db);

        Assert.Empty(await store.ListExistingAsync(CancellationToken.None));

        var plan = ShippingMethodBackfillPlanner.Plan(await CatalogAsync(db), await store.ListExistingAsync(CancellationToken.None));

        Assert.True(plan.IsExecutable, string.Join("; ", plan.Violations));
        Assert.All(plan.Items, x => Assert.Equal(ShippingMethodPlanAction.Create, x.Action));
        // Live Development shape: post(2 options) + tipax(2 options) + 3 option-less services = 7.
        Assert.Equal(7, plan.Items.Count);
        Assert.Empty(await store.ListExistingAsync(CancellationToken.None));
        Assert.Empty(await db.ShippingMethods.AsNoTracking().ToListAsync());

        var desired = plan.Items.ToDictionary(x => x.Desired.MethodCode, x => x.Desired, StringComparer.Ordinal);
        Assert.Equal("standard", desired["post:standard"].LegacyOptionCode);
        Assert.True(desired["post:standard"].IsActive);
        Assert.False(desired["post:express"].IsActive);
        Assert.Null(desired["snapp_courier"].LegacyOptionCode);
        Assert.False(desired["snapp_courier"].IsActive);
        Assert.Null(desired["store_courier"].LegacyOptionCode);
        Assert.True(desired["store_courier"].IsActive);
    }

    [Fact]
    public async Task Each_language_keeps_its_own_name_in_the_planned_translations()
    {
        await using var db = CreateDb();
        SeedLegacyCatalog(db);

        var plan = ShippingMethodBackfillPlanner.Plan(await CatalogAsync(db), []);
        var standard = plan.Items.Single(x => x.Desired.MethodCode == "post:standard").Desired;

        Assert.Equal("معمولی", standard.Translations.Single(x => x.LanguageId == LangFa).Name);
        Assert.Equal("Standard", standard.Translations.Single(x => x.LanguageId == LangEn).Name);
        Assert.Equal(2, standard.Translations.Count);
    }

    [Fact]
    public async Task A_violating_plan_is_refused_before_any_transaction_or_write()
    {
        await using var db = CreateDb();
        SeedLegacyCatalog(db);
        var store = Store(db);

        var conflicting = new ShippingMethodBackfillPlan(
            [new ShippingMethodPlanItem(
                ShippingMethodPlanAction.Create,
                ShippingMethodBackfillPlanner.Plan(await CatalogAsync(db), []).Items[0].Desired,
                null)],
            ["binding_mismatch:post:standard"]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => store.ApplyAsync(conflicting, CancellationToken.None));

        Assert.Empty(await db.ShippingMethods.AsNoTracking().ToListAsync());
        Assert.Empty(await db.ShippingMethodTranslations.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task An_align_activation_plan_targets_the_existing_row_without_touching_identity()
    {
        await using var db = CreateDb();
        SeedLegacyCatalog(db);
        var store = Store(db);

        // Postgres is the only environment that proves the transactional apply path; the InMemory
        // provider has no transactions, so this fact proves the decision side only (identity is never
        // part of an AlignActivation item) and the real apply is proven in the isolated DB evidence.
        var existing = new[]
        {
            new ShippingMethodExistingRow(Guid.Parse("44444444-0000-7000-8000-000000000001"),
                "snapp_courier", "snapp_courier", null, IsActive: true),
        };
        var plan = ShippingMethodBackfillPlanner.Plan(await CatalogAsync(db), existing);

        var snapp = plan.Items.Single(x => x.Desired.MethodCode == "snapp_courier");
        Assert.Equal(ShippingMethodPlanAction.AlignActivation, snapp.Action);
        Assert.Equal(existing[0].ShippingMethodId, snapp.ExistingShippingMethodId);
        Assert.Equal("snapp_courier", snapp.Desired.LegacyServiceCode);
        Assert.Null(snapp.Desired.LegacyOptionCode);
        Assert.Empty(await store.ListExistingAsync(CancellationToken.None));
    }

    private static ShippingOneLevelBackfillStore Store(FulfillmentDbContext db) =>
        new(db, new ShippingCatalogReader(db), new FixedClock(DateTimeOffset.Parse("2026-10-15T00:00:00Z")),
            new FixedIdGenerator(Enumerable.Range(0, 64)
                .Select(i => Guid.Parse($"33333333-0000-7000-8000-{i:D12}")).ToArray()));

    private static Task<IReadOnlyList<ShippingCatalogServiceSnapshot>> CatalogAsync(FulfillmentDbContext db) =>
        new ShippingCatalogReader(db).ListAsync(CancellationToken.None);

    private static FulfillmentDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<FulfillmentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);

    private static void SeedLegacyCatalog(FulfillmentDbContext db)
    {
        var now = DateTimeOffset.Parse("2026-10-15T00:00:00Z");
        var post = Guid.Parse("11111111-0000-7000-8000-000000000001");
        var tipax = Guid.Parse("11111111-0000-7000-8000-000000000002");
        var snapp = Guid.Parse("11111111-0000-7000-8000-000000000005");
        var storeCourier = Guid.Parse("11111111-0000-7000-8000-000000000003");
        var inPerson = Guid.Parse("11111111-0000-7000-8000-000000000004");

        db.ShippingServices.AddRange(
            ShippingService.Create(post, "post", "post", "post", "blue", 10, true, now),
            ShippingService.Create(tipax, "tipax", "tipax", "tipax", "amber", 20, true, now),
            ShippingService.Create(snapp, "snapp_courier", "courier", "courier", "emerald", 30, false, now),
            ShippingService.Create(storeCourier, "store_courier", "store_courier", "bike", "violet", 40, true, now),
            ShippingService.Create(inPerson, "in_person", "in_person", "store", "rose", 50, true, now));

        foreach (var service in new[] { post, tipax, snapp, storeCourier, inPerson })
        {
            db.ShippingServiceTranslations.Add(ShippingServiceTranslation.Create(Guid.NewGuid(), service, LangFa, "نام", null));
            db.ShippingServiceTranslations.Add(ShippingServiceTranslation.Create(Guid.NewGuid(), service, LangEn, "Name", null));
        }

        AddOption(db, post, "express", 10, false, "پیشتاز", "Express", now);
        AddOption(db, post, "standard", 20, true, "معمولی", "Standard", now);
        AddOption(db, tipax, "express", 10, true, "پیشتاز", "Express", now);
        AddOption(db, tipax, "standard", 20, true, "معمولی", "Standard", now);

        db.SaveChanges();
    }

    private static void AddOption(
        FulfillmentDbContext db, Guid serviceId, string code, int sort, bool active, string fa, string en, DateTimeOffset now)
    {
        var id = Guid.NewGuid();
        db.ShippingServiceOptions.Add(ShippingServiceOption.Create(id, serviceId, code, sort, active, now));
        db.ShippingServiceOptionTranslations.Add(ShippingServiceOptionTranslation.Create(Guid.NewGuid(), id, LangFa, fa));
        db.ShippingServiceOptionTranslations.Add(ShippingServiceOptionTranslation.Create(Guid.NewGuid(), id, LangEn, en));
    }
}
