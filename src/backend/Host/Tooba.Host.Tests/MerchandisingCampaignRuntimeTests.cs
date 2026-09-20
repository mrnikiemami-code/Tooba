using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Inventory.Application;
using Tooba.Inventory.Domain;
using Tooba.Inventory.Infrastructure;
using Tooba.Inventory.Infrastructure.Persistence;
using Tooba.Offer.Application.Ports;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;
using Tooba.Offer.Domain;
using Tooba.Offer.Infrastructure;
using Tooba.Offer.Infrastructure.Outbox;
using Tooba.Offer.Infrastructure.Adapters;
using Tooba.Offer.Infrastructure.Persistence;
using Tooba.Party.Infrastructure;
using Tooba.Party.Infrastructure.Persistence;
using Tooba.Persistence;
using Tooba.Pricing.Application;
using Tooba.Pricing.Contracts;
using Tooba.Pricing.Domain;
using Tooba.Pricing.Infrastructure;
using Tooba.Pricing.Infrastructure.Persistence;
using Tooba.Promotion.Application;
using Tooba.Promotion.Domain;
using Tooba.Promotion.Infrastructure;
using Tooba.Promotion.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// پوشش runtime resolver کمپین مرچندایزینگ: انتخاب، آینده، موجودی، locale، قیمت، seed.
/// </summary>
[Collection("PostgresSerial")]
public sealed class MerchandisingCampaignRuntimeTests : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private bool _dockerAvailable;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("tooba_merch_runtime")
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

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    /// <summary>
    /// موارد انتخاب active/future، فیلتر OOS، locale، قیمت، ترتیب، seed idempotent، مرز فروشگاه.
    /// </summary>
    [SkippableFact]
    public async Task Merchandising_campaign_runtime_selection_availability_locale_price_and_seed()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        var cs = _container.GetConnectionString();
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("tenant-merch-rt", "tenant-merch-rt"));

        await using var catalogDb = CreateCatalogDb(cs, commerce);
        await using var partyDb = CreatePartyDb(cs, commerce);
        await using var offerDb = CreateOfferDb(cs, commerce);
        await using var pricingDb = CreatePricingDb(cs, commerce);
        await using var inventoryDb = CreateInventoryDb(cs, commerce);
        await using var promoDb = CreatePromotionDb(cs, commerce);
        await catalogDb.Database.MigrateAsync();
        await partyDb.Database.MigrateAsync();
        await offerDb.Database.MigrateAsync();
        await pricingDb.Database.MigrateAsync();
        await inventoryDb.Database.MigrateAsync();
        await promoDb.Database.MigrateAsync();

        var catalogDir = new CatalogDirectory(catalogDb, new OpenCatalogUseCaseGuard());
        var partyDir = new PartyDirectory(partyDb);
        var offerDir = new OfferDirectory(offerDb, new OpenOfferUseCaseGuard(), catalogDir, partyDir, new SystemUtcClock(), new UuidV7IdGenerator());
        var priceDir = new PriceDirectory(pricingDb, new OpenPricingUseCaseGuard(), offerDir);
        var inventoryDir = new InventoryDirectory(inventoryDb, new OpenInventoryUseCaseGuard(), offerDir, catalogDir);
        var merchDir = new MerchandisingCampaignDirectory(promoDb);
        var query = new MerchandisingCampaignQuery(promoDb, offerDir, priceDir, inventoryDir);

        var storeA = Guid.Parse("aaaaaaaa-aaaa-7aaa-8aaa-aaaaaaaaaaa1");
        var storeB = Guid.Parse("bbbbbbbb-bbbb-7bbb-8bbb-bbbbbbbbbbb1");
        var now = DateTimeOffset.Parse("2026-09-19T12:00:00Z");

        var type = await merchDir.EnsureAmazingTypeSeededAsync(CancellationToken.None);

        var names = new Dictionary<string, string> { ["fa-IR"] = "کالا", ["en-US"] = "Item" };
        var product = await catalogDir.CreateProductAsync(
            CatalogProductKind.PhysicalGood,
            "merch-rt-item",
            null,
            names,
            CancellationToken.None);
        var colorId = await catalogDir.CreateAttributeDefinitionAsync(
            "color",
            CatalogAttributeValueKind.Enumeration,
            isVariantAxis: true,
            new Dictionary<string, string> { ["fa-IR"] = "رنگ" },
            CancellationToken.None);
        var black = await catalogDir.AddAttributeOptionAsync(
            colorId,
            "black",
            new Dictionary<string, string> { ["fa-IR"] = "سیاه" },
            CancellationToken.None);
        var variant = await catalogDir.CreateVariantAsync(
            product.ProductId,
            "MRT-1",
            [(colorId, "ignored", black)],
            CancellationToken.None);

        var sellers = new List<Guid>();
        var offers = new List<OfferReference>();
        for (var i = 0; i < 5; i++)
        {
            var seller = await partyDir.CreateOrganizationAsync($"فروشنده RT {i}", null, CancellationToken.None);
            sellers.Add(seller.PartyId);
            var offer = await offerDir.CreateOfferAsync(
                variant.VariantId,
                seller.PartyId,
                SalesChannel.Marketplace,
                $"MRT-{i}",
                CancellationToken.None);
            await offerDir.ActivateAsync(offer.OfferId, CancellationToken.None);
            offers.Add(offer);
            var price = await priceDir.CreatePriceAsync(
                offer.OfferId,
                "IR",
                SalesChannel.Marketplace,
                100000 + (i * 1000),
                "IRR",
                now.AddMonths(-1),
                null,
                CancellationToken.None);
            await priceDir.ActivateAsync(price.PriceId, CancellationToken.None);
        }

        var loc = await inventoryDir.CreateLocationAsync("WH-MRT", "انبار RT", CancellationToken.None);
        for (var i = 0; i < 4; i++)
        {
            var stock = await inventoryDir.OpenPositionAsync(offers[i].OfferId, loc, CancellationToken.None);
            await inventoryDir.AdjustAsync(
                stock,
                StockAdjustmentKind.Increase,
                10,
                "seed",
                null,
                CancellationToken.None);
        }

        // offer[4] deliberately OOS (open position, no increase)
        await inventoryDir.OpenPositionAsync(offers[4].OfferId, loc, CancellationToken.None);

        // Suspended / non-marketable offer
        var suspendedSeller = await partyDir.CreateOrganizationAsync("فروشنده معلق", null, CancellationToken.None);
        var suspended = await offerDir.CreateOfferAsync(
            variant.VariantId,
            suspendedSeller.PartyId,
            SalesChannel.Marketplace,
            "MRT-SUS",
            CancellationToken.None);
        await offerDir.ActivateAsync(suspended.OfferId, CancellationToken.None);
        await offerDir.SuspendAsync(suspended.OfferId, CancellationToken.None);
        var susStock = await inventoryDir.OpenPositionAsync(suspended.OfferId, loc, CancellationToken.None);
        await inventoryDir.AdjustAsync(susStock, StockAdjustmentKind.Increase, 5, "seed", null, CancellationToken.None);

        // Order limits on first offer
        var tracked = await offerDb.Offers.SingleAsync(x => x.OfferId == offers[0].OfferId);
        tracked.SetOrderQuantityLimits(1, 3, now);
        await offerDb.SaveChangesAsync();

        var primary = await merchDir.UpsertSeedCampaignAsync(
            Guid.Parse("019a16a0-0001-7000-8000-000000000001"),
            type.Id,
            storeA,
            now.AddHours(-2),
            now.AddDays(2),
            priority: 100,
            MerchandisingCampaignLifecycleStatus.Published,
            CancellationToken.None);
        await merchDir.UpsertCampaignTranslationAsync(
            primary.Id,
            "fa-IR",
            "[DEV-SEED] Amazing Active Primary | عنوان فارسی",
            null,
            "بج",
            CancellationToken.None);
        await merchDir.UpsertCampaignTranslationAsync(
            primary.Id,
            "en-US",
            "[DEV-SEED] Amazing Active Primary",
            "English subtitle",
            "Badge",
            CancellationToken.None);
        await merchDir.SyncSeedMembersAsync(
            primary.Id,
            storeA,
            [
                offers[2].OfferId,
                offers[0].OfferId,
                offers[1].OfferId,
                offers[3].OfferId,
                offers[4].OfferId,
                suspended.OfferId,
            ],
            CancellationToken.None);

        // R18: campaign-scoped AuthoredPrice for first three members; offers[3] stays base-only.
        foreach (var (offer, promo) in new[]
                 {
                     (offers[0], 70000m),
                     (offers[1], 71000m),
                     (offers[2], 72000m),
                 })
        {
            var campaignPrice = await priceDir.CreateCampaignPriceAsync(
                offer.OfferId,
                primary.Id,
                "IR",
                SalesChannel.Marketplace,
                promo,
                "IRR",
                now.AddHours(-2),
                now.AddDays(2),
                CancellationToken.None);
            await priceDir.ActivateAsync(campaignPrice.PriceId, CancellationToken.None);
        }

        // Future campaign price must not leak through future resolver.
        var futurePromo = await priceDir.CreateCampaignPriceAsync(
            offers[0].OfferId,
            Guid.Parse("019a16a0-0003-7000-8000-000000000003"),
            "IR",
            SalesChannel.Marketplace,
            50000m,
            "IRR",
            now.AddDays(1),
            now.AddDays(2),
            CancellationToken.None);
        await priceDir.ActivateAsync(futurePromo.PriceId, CancellationToken.None);

        var loser = await merchDir.UpsertSeedCampaignAsync(
            Guid.Parse("019a16a0-0002-7000-8000-000000000002"),
            type.Id,
            storeA,
            now.AddHours(-1),
            now.AddDays(1),
            priority: 10,
            MerchandisingCampaignLifecycleStatus.Published,
            CancellationToken.None);
        await merchDir.UpsertCampaignTranslationAsync(
            loser.Id,
            "en-US",
            "[DEV-SEED] Amazing Active Loser",
            null,
            null,
            CancellationToken.None);
        await merchDir.SyncSeedMembersAsync(loser.Id, storeA, [offers[0].OfferId], CancellationToken.None);

        var future = await merchDir.UpsertSeedCampaignAsync(
            Guid.Parse("019a16a0-0003-7000-8000-000000000003"),
            type.Id,
            storeA,
            now.AddDays(1),
            now.AddDays(2),
            priority: 80,
            MerchandisingCampaignLifecycleStatus.Published,
            CancellationToken.None);
        await merchDir.UpsertCampaignTranslationAsync(
            future.Id,
            "en-US",
            "[DEV-SEED] Amazing Future Teasing",
            null,
            null,
            CancellationToken.None);
        await merchDir.SyncSeedMembersAsync(future.Id, storeA, [offers[0].OfferId], CancellationToken.None);

        var expired = await merchDir.UpsertSeedCampaignAsync(
            Guid.Parse("019a16a0-0004-7000-8000-000000000004"),
            type.Id,
            storeA,
            now.AddDays(-3),
            now.AddDays(-1),
            priority: 90,
            MerchandisingCampaignLifecycleStatus.Published,
            CancellationToken.None);
        _ = expired;

        var draft = await merchDir.UpsertSeedCampaignAsync(
            Guid.Parse("019a16a0-0005-7000-8000-000000000005"),
            type.Id,
            storeA,
            now.AddHours(-1),
            now.AddDays(1),
            priority: 50,
            MerchandisingCampaignLifecycleStatus.Draft,
            CancellationToken.None);
        _ = draft;

        var archived = await merchDir.UpsertSeedCampaignAsync(
            Guid.Parse("019a16a0-0006-7000-8000-000000000006"),
            type.Id,
            storeA,
            now.AddHours(-1),
            now.AddDays(1),
            priority: 70,
            MerchandisingCampaignLifecycleStatus.Archived,
            CancellationToken.None);
        _ = archived;

        // Active selection: priority wins
        var active = await query.ResolveActiveByTypeAsync(
            storeA,
            MerchandisingPromotionType.AmazingCode,
            "en-US",
            now,
            take: 48,
            MerchandisingPriceScope.Default,
            CancellationToken.None);
        Assert.NotNull(active);
        Assert.Equal(primary.Id, active!.CampaignId);
        Assert.Equal("[DEV-SEED] Amazing Active Primary", active.Title);
        Assert.Equal("English subtitle", active.Subtitle);
        Assert.False(active.IsTeasing);
        Assert.NotNull(active.RemainingDuration);

        // OOS + suspended filtered; order preserved for remaining
        Assert.Equal(
            new[] { offers[2].OfferId, offers[0].OfferId, offers[1].OfferId, offers[3].OfferId },
            active.Members.Select(x => x.SellerOfferId).ToArray());
        Assert.DoesNotContain(active.Members, x => x.SellerOfferId == offers[4].OfferId);
        Assert.DoesNotContain(active.Members, x => x.SellerOfferId == suspended.OfferId);

        // Canonical price + qty limits + campaign promo compare-at
        var firstMember = active.Members.Single(x => x.SellerOfferId == offers[0].OfferId);
        Assert.Equal(70000m, firstMember.PriceAmount);
        Assert.Equal(100000m, firstMember.CompareAtAmount);
        Assert.Equal("IRR", firstMember.PriceCurrency);
        Assert.Equal(1m, firstMember.MinimumOrderQuantity);
        Assert.Equal(3m, firstMember.MaximumOrderQuantity);
        Assert.True(firstMember.AvailableQuantity > 0);
        Assert.DoesNotContain("PromoAmount", typeof(MerchandisingCampaignMemberRuntimeModel).GetProperties().Select(p => p.Name));

        var baseOnlyMember = active.Members.Single(x => x.SellerOfferId == offers[3].OfferId);
        Assert.Equal(103000m, baseOnlyMember.PriceAmount);
        Assert.Null(baseOnlyMember.CompareAtAmount);

        // Base resolution unchanged without campaign context
        var baseQuote = await priceDir.ResolvePriceAsync(
            new PriceResolutionQuery(offers[0].OfferId, "IR", SalesChannel.Marketplace, "IRR", now, null, null, null),
            CancellationToken.None);
        Assert.Equal(100000m, baseQuote!.Amount);

        // FA locale
        var activeFa = await query.ResolveActiveByTypeAsync(
            storeA,
            MerchandisingPromotionType.AmazingCode,
            "fa-IR",
            now,
            10,
            null,
            CancellationToken.None);
        Assert.Contains("عنوان فارسی", activeFa!.Title);

        // Fallback locale (de → fa)
        var activeDe = await query.ResolveActiveByTypeAsync(
            storeA,
            MerchandisingPromotionType.AmazingCode,
            "de-DE",
            now,
            10,
            null,
            CancellationToken.None);
        Assert.Contains("عنوان فارسی", activeDe!.Title);

        // Future excluded from active; future resolver works
        var futureModel = await query.ResolveFutureByTypeAsync(
            storeA,
            MerchandisingPromotionType.AmazingCode,
            "en-US",
            now,
            10,
            null,
            CancellationToken.None);
        Assert.NotNull(futureModel);
        Assert.Equal(future.Id, futureModel!.CampaignId);
        Assert.True(futureModel.IsTeasing);
        Assert.NotEqual(active.CampaignId, futureModel.CampaignId);
        // Future membership projection must not apply campaign promo prices.
        Assert.All(futureModel.Members, m => Assert.Null(m.CompareAtAmount));
        var futureMember0 = futureModel.Members.FirstOrDefault(x => x.SellerOfferId == offers[0].OfferId);
        if (futureMember0 is not null)
        {
            Assert.Equal(100000m, futureMember0.PriceAmount);
        }

        // Expired / draft / archived not active at fixed clock `now`
        Assert.False((await promoDb.MerchandisingCampaigns.SingleAsync(x => x.Id == expired.Id)).IsRuntimeActive(now));
        Assert.False((await promoDb.MerchandisingCampaigns.SingleAsync(x => x.Id == draft.Id)).IsRuntimeActive(now));
        Assert.False((await promoDb.MerchandisingCampaigns.SingleAsync(x => x.Id == archived.Id)).IsRuntimeActive(now));
        Assert.Null(await query.ResolveActiveByTypeAsync(
            storeA,
            MerchandisingPromotionType.AmazingCode,
            "en-US",
            now.AddDays(30),
            5,
            null,
            CancellationToken.None));

        // StartAt tiebreaker when priority equal
        var tieLow = await merchDir.UpsertSeedCampaignAsync(
            Guid.Parse("019a16a0-0010-7000-8000-000000000010"),
            type.Id,
            storeA,
            now.AddHours(-5),
            now.AddDays(1),
            priority: 55,
            MerchandisingCampaignLifecycleStatus.Published,
            CancellationToken.None);
        var tieHigh = await merchDir.UpsertSeedCampaignAsync(
            Guid.Parse("019a16a0-0011-7000-8000-000000000011"),
            type.Id,
            storeA,
            now.AddHours(-1),
            now.AddDays(1),
            priority: 55,
            MerchandisingCampaignLifecycleStatus.Published,
            CancellationToken.None);
        await merchDir.ArchiveCampaignAsync(primary.Id, CancellationToken.None);
        await merchDir.ArchiveCampaignAsync(loser.Id, CancellationToken.None);
        var tieWinner = await query.ResolveActiveByTypeAsync(
            storeA,
            MerchandisingPromotionType.AmazingCode,
            "en-US",
            now,
            5,
            null,
            CancellationToken.None);
        Assert.Equal(tieHigh.Id, tieWinner!.CampaignId);
        Assert.NotEqual(tieLow.Id, tieWinner.CampaignId);

        // Cross-store: members for storeB empty; active for storeB null
        Assert.Null(await query.ResolveActiveByTypeAsync(
            storeB,
            MerchandisingPromotionType.AmazingCode,
            "en-US",
            now,
            5,
            null,
            CancellationToken.None));
        Assert.Empty(await query.ResolveCampaignMembersAsync(
            tieHigh.Id,
            storeB,
            "en-US",
            now,
            5,
            null,
            CancellationToken.None));

        // Seed idempotent — second upsert keeps same Ids / no duplicate campaigns for markers
        await merchDir.UpsertSeedCampaignAsync(
            Guid.Parse("019a16a0-0001-7000-8000-000000000001"),
            type.Id,
            storeA,
            now.AddHours(-2),
            now.AddDays(2),
            priority: 100,
            MerchandisingCampaignLifecycleStatus.Published,
            CancellationToken.None);
        Assert.Equal(
            1,
            await promoDb.MerchandisingCampaigns.CountAsync(x =>
                x.Id == Guid.Parse("019a16a0-0001-7000-8000-000000000001")));

        // Max take bound
        Assert.Equal(48, MerchandisingCampaignRuntimeLimits.MaxMemberTake);
        Assert.Equal(
            typeof(PriceQualifierKind).GetEnumNames(),
            new[] { nameof(PriceQualifierKind.Base), nameof(PriceQualifierKind.MerchandisingCampaign) });

        // Recovery markers
        var root = FindRepoRoot();
        Assert.True(File.Exists(Path.Combine(root, "docs", "ai", "tasks", "TB-P10-T022-R16.task.md")));
        Assert.True(File.Exists(Path.Combine(root, "docs", "evidence", "TB-P10-T022-R15", "recovery-start.md")));
    }

    private static PromotionDbContext CreatePromotionDb(string connectionString, ICurrentCommerceContext commerce)
    {
        var modules = new IOutboxModuleRegistration[] { new PromotionOutboxRegistration() };
        var serializer = new JsonIntegrationEventSerializer(modules);
        var interceptor = new OutboxSaveChangesInterceptor(commerce, modules, serializer);
        var options = new DbContextOptionsBuilder<PromotionDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, PromotionDbContext.Schema, typeof(PromotionDbContext));
        options.AddInterceptors(interceptor);
        return new PromotionDbContext(options.Options);
    }

    private static CatalogDbContext CreateCatalogDb(string connectionString, ICurrentCommerceContext commerce)
    {
        var modules = new IOutboxModuleRegistration[] { new CatalogOutboxRegistration() };
        var serializer = new JsonIntegrationEventSerializer(modules);
        var interceptor = new OutboxSaveChangesInterceptor(commerce, modules, serializer);
        var options = new DbContextOptionsBuilder<CatalogDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, CatalogDbContext.Schema, typeof(CatalogDbContext));
        options.AddInterceptors(interceptor);
        return new CatalogDbContext(options.Options);
    }

    private static PartyDbContext CreatePartyDb(string connectionString, ICurrentCommerceContext commerce)
    {
        var modules = new IOutboxModuleRegistration[] { new PartyOutboxRegistration() };
        var serializer = new JsonIntegrationEventSerializer(modules);
        var interceptor = new OutboxSaveChangesInterceptor(commerce, modules, serializer);
        var options = new DbContextOptionsBuilder<PartyDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, PartyDbContext.Schema, typeof(PartyDbContext));
        options.AddInterceptors(interceptor);
        return new PartyDbContext(options.Options);
    }

    private static OfferDbContext CreateOfferDb(string connectionString, ICurrentCommerceContext commerce)
    {
        var modules = new IOutboxModuleRegistration[] { new OfferOutboxRegistration() };
        var serializer = new JsonIntegrationEventSerializer(modules);
        var interceptor = new OutboxSaveChangesInterceptor(commerce, modules, serializer);
        var options = new DbContextOptionsBuilder<OfferDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, OfferDbContext.Schema, typeof(OfferDbContext));
        options.AddInterceptors(interceptor);
        return new OfferDbContext(options.Options);
    }

    private static PricingDbContext CreatePricingDb(string connectionString, ICurrentCommerceContext commerce)
    {
        var modules = new IOutboxModuleRegistration[] { new PricingOutboxRegistration() };
        var serializer = new JsonIntegrationEventSerializer(modules);
        var interceptor = new OutboxSaveChangesInterceptor(commerce, modules, serializer);
        var options = new DbContextOptionsBuilder<PricingDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, PricingDbContext.Schema, typeof(PricingDbContext));
        options.AddInterceptors(interceptor);
        return new PricingDbContext(options.Options);
    }

    private static InventoryDbContext CreateInventoryDb(string connectionString, ICurrentCommerceContext commerce)
    {
        var modules = new IOutboxModuleRegistration[] { new InventoryOutboxRegistration() };
        var serializer = new JsonIntegrationEventSerializer(modules);
        var interceptor = new OutboxSaveChangesInterceptor(commerce, modules, serializer);
        var options = new DbContextOptionsBuilder<InventoryDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, InventoryDbContext.Schema, typeof(InventoryDbContext));
        options.AddInterceptors(interceptor);
        return new InventoryDbContext(options.Options);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
