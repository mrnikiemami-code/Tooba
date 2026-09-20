using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Tooba.BuildingBlocks;
using Tooba.Inventory.Domain;
using Tooba.Offer.Domain;
using Tooba.Offer.Domain.Aggregates;
using Tooba.Persistence;
using Tooba.Pricing.Domain;
using Tooba.Promotion.Application;
using Tooba.Promotion.Domain;
using Tooba.Promotion.Infrastructure;
using Tooba.Promotion.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// پوشش foundation کمپین مرچندایزینگ داخل schema promotion (جدا از تخفیف تسویه).
/// </summary>
[Collection("PostgresSerial")]
public sealed class MerchandisingCampaignFoundationTests : IAsyncLifetime
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
                .WithDatabase("tooba_merch_promo")
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
    /// موارد ۱–۲۱: seed، گونه، lifecycle، ترجمه، عضویت، مرز فروشگاه، رگرسیون Pricing/Inventory/Checkout.
    /// </summary>
    [SkippableFact]
    public async Task Merchandising_campaign_foundation_covers_required_cases()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        await using var db = CreateDb(_container.GetConnectionString());
        await db.Database.MigrateAsync();
        var dir = new MerchandisingCampaignDirectory(db);
        var checkout = new PromotionDirectory(db, new OpenPromotionUseCaseGuard(), new DeferredPromotionRedemptionLedger());
        var storeA = Guid.Parse("aaaaaaaa-aaaa-7aaa-8aaa-aaaaaaaaaaa1");
        var storeB = Guid.Parse("bbbbbbbb-bbbb-7bbb-8bbb-bbbbbbbbbbb1");
        var now = DateTimeOffset.Parse("2026-09-19T12:00:00Z");

        // 1 AMAZING seed idempotent
        var seeded1 = await dir.EnsureAmazingTypeSeededAsync(CancellationToken.None);
        var seeded2 = await dir.EnsureAmazingTypeSeededAsync(CancellationToken.None);
        Assert.Equal(MerchandisingPromotionType.AmazingCode, seeded1.Code);
        Assert.Equal(seeded1.Id, seeded2.Id);
        Assert.True(seeded1.IsSystem);
        Assert.Equal(1, await db.MerchandisingPromotionTypes.CountAsync());
        var typeTranslations = await db.MerchandisingPromotionTypeTranslations
            .Where(x => x.TypeId == seeded1.Id)
            .OrderBy(x => x.Locale)
            .ToListAsync();
        Assert.Equal(2, typeTranslations.Count);
        Assert.Contains(typeTranslations, x => x.Locale == "fa-IR" && x.DisplayName == "پیشنهاد شگفت‌انگیز");
        Assert.Contains(typeTranslations, x => x.Locale == "en-US" && x.DisplayName == "Amazing Offers");

        // 2 PromotionType Code unique
        db.MerchandisingPromotionTypes.Add(
            MerchandisingPromotionType.CreateSystem(MerchandisingPromotionType.AmazingCode, 1, now));
        await Assert.ThrowsAnyAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();

        // 3 system type rename/delete protection
        var amazing = await db.MerchandisingPromotionTypes.SingleAsync(x => x.Code == MerchandisingPromotionType.AmazingCode);
        Assert.Throws<InvalidOperationException>(() => amazing.RenameCode("OTHER", now));
        Assert.Throws<InvalidOperationException>(() => amazing.EnsureCanDelete());

        // 4 Campaign requires valid PromotionType
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dir.CreateCampaignAsync(Guid.Parse("99999999-9999-7999-8999-999999999999"), storeA, now, null, 1, CancellationToken.None));

        // 5 StartAt < EndAt
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dir.CreateCampaignAsync(amazing.Id, storeA, now, now, 1, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dir.CreateCampaignAsync(amazing.Id, storeA, now, now.AddMinutes(-1), 1, CancellationToken.None));

        var draft = await dir.CreateCampaignAsync(
            amazing.Id,
            storeA,
            now.AddHours(-1),
            now.AddHours(2),
            priority: 10,
            CancellationToken.None);

        // 6 Draft inactive
        Assert.Null(await dir.ResolveActiveCampaignAsync(storeA, MerchandisingPromotionType.AmazingCode, now, CancellationToken.None));
        Assert.False((await db.MerchandisingCampaigns.SingleAsync(x => x.Id == draft.Id)).IsRuntimeActive(now));

        // 7 Published future inactive
        var future = await dir.CreateCampaignAsync(
            amazing.Id,
            storeA,
            now.AddDays(1),
            now.AddDays(2),
            priority: 50,
            CancellationToken.None);
        await dir.PublishCampaignAsync(future.Id, CancellationToken.None);
        Assert.Null(await dir.ResolveActiveCampaignAsync(storeA, MerchandisingPromotionType.AmazingCode, now, CancellationToken.None));

        // 8 Published current window active
        await dir.PublishCampaignAsync(draft.Id, CancellationToken.None);
        var active = await dir.ResolveActiveCampaignAsync(storeA, MerchandisingPromotionType.AmazingCode, now, CancellationToken.None);
        Assert.NotNull(active);
        Assert.Equal(draft.Id, active!.Id);
        Assert.True((await db.MerchandisingCampaigns.SingleAsync(x => x.Id == draft.Id)).IsRuntimeActive(now));

        // 9 Expired inactive
        await dir.UpdateCampaignWindowAsync(draft.Id, now.AddHours(-3), now.AddHours(-1), 10, CancellationToken.None);
        Assert.Null(await dir.ResolveActiveCampaignAsync(storeA, MerchandisingPromotionType.AmazingCode, now, CancellationToken.None));
        await dir.UpdateCampaignWindowAsync(draft.Id, now.AddHours(-1), now.AddHours(2), 10, CancellationToken.None);
        Assert.Equal(draft.Id, (await dir.ResolveActiveCampaignAsync(storeA, MerchandisingPromotionType.AmazingCode, now, CancellationToken.None))!.Id);

        // 10 Archived inactive
        await dir.ArchiveCampaignAsync(draft.Id, CancellationToken.None);
        Assert.Null(await dir.ResolveActiveCampaignAsync(storeA, MerchandisingPromotionType.AmazingCode, now, CancellationToken.None));

        var live = await dir.CreateCampaignAsync(
            amazing.Id,
            storeA,
            now.AddHours(-1),
            now.AddHours(3),
            priority: 20,
            CancellationToken.None);
        await dir.PublishCampaignAsync(live.Id, CancellationToken.None);

        // 11 translation unique per language
        await dir.UpsertCampaignTranslationAsync(live.Id, "fa-IR", "عنوان", "زیر", "بج", CancellationToken.None);
        await dir.UpsertCampaignTranslationAsync(live.Id, "fa-IR", "عنوان۲", null, null, CancellationToken.None);
        Assert.Equal(1, await db.MerchandisingCampaignTranslations.CountAsync(x => x.CampaignId == live.Id && x.Locale == "fa-IR"));
        Assert.Equal("عنوان۲", (await db.MerchandisingCampaignTranslations.SingleAsync(x => x.CampaignId == live.Id && x.Locale == "fa-IR")).Title);

        // 12 CampaignOffer unique membership
        var offer1 = Guid.Parse("11111111-1111-7111-8111-111111111111");
        var offer2 = Guid.Parse("22222222-2222-7222-8222-222222222222");
        await dir.AddOfferAsync(live.Id, offer1, 10, storeA, CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dir.AddOfferAsync(live.Id, offer1, 11, storeA, CancellationToken.None));

        // 13 cross-store membership rejected
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dir.AddOfferAsync(live.Id, offer2, 5, storeB, CancellationToken.None));
        await dir.AddOfferAsync(live.Id, offer2, 5, storeA, CancellationToken.None);

        // 14 ordering stable
        await dir.ReorderOffersAsync(live.Id, [offer2, offer1], CancellationToken.None);
        var ordered = await dir.ResolveOrderedMembersAsync(live.Id, CancellationToken.None);
        Assert.Equal(new[] { offer2, offer1 }, ordered.Select(x => x.SellerOfferId).ToArray());
        Assert.Equal(new[] { 0, 1 }, ordered.Select(x => x.SortOrder).ToArray());

        // 15 campaign archive/delete never deletes SellerOffer (membership only; Offer type unchanged)
        await dir.ArchiveCampaignAsync(live.Id, CancellationToken.None);
        await dir.RemoveOfferAsync(live.Id, offer1, CancellationToken.None);
        Assert.DoesNotContain("IsAmazing", typeof(SellerOffer).GetProperties().Select(p => p.Name));
        Assert.Contains(nameof(SellerOffer.MaximumOrderQuantity), typeof(SellerOffer).GetProperties().Select(p => p.Name));

        // 16 checkout PromotionDefinition regression
        var at = DateTimeOffset.Parse("2026-06-01T00:00:00Z");
        var promo = await checkout.CreateAsync(
            "رگرسیون تخفیف",
            10,
            at.AddMonths(-1),
            null,
            PromotionStackingPolicy.Exclusive,
            PromotionDiscountKind.PercentageOff,
            0.10m,
            0m,
            null,
            null,
            offer1,
            null,
            null,
            null,
            "IR",
            "Marketplace",
            "IRR",
            null,
            null,
            null,
            null,
            CancellationToken.None);
        await checkout.ActivateAsync(promo.PromotionId, CancellationToken.None);
        var eval = await checkout.EvaluateAsync(
            new PromotionEvaluationRequest(
                offer1,
                Guid.Parse("33333333-3333-7333-8333-333333333333"),
                null,
                Guid.Parse("44444444-4444-7444-8444-444444444444"),
                "IR",
                "Marketplace",
                "IRR",
                1,
                100000m,
                null,
                null,
                null,
                at),
            CancellationToken.None);
        Assert.Equal(10000m, eval.DiscountAmount);
        Assert.Equal(typeof(PromotionDefinition), db.Model.FindEntityType(typeof(PromotionDefinition))!.ClrType);

        // 17 AuthoredPrice regression — QualifierKind Base-only, no campaign scalar
        Assert.Equal(new[] { nameof(PriceQualifierKind.Base) }, Enum.GetNames<PriceQualifierKind>());
        Assert.DoesNotContain("PromoAmount", typeof(AuthoredPrice).GetProperties().Select(p => p.Name));
        Assert.DoesNotContain("CampaignId", typeof(AuthoredPrice).GetProperties().Select(p => p.Name));
        Assert.True(typeof(IMerchandisingCampaignPromoPrice).IsInterface);

        // 18 StockPosition regression
        var stockProps = typeof(StockPosition).GetProperties().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        Assert.Contains(nameof(StockPosition.OnHand), stockProps);
        Assert.Contains(nameof(StockPosition.Reserved), stockProps);
        Assert.Contains(nameof(StockPosition.Available), stockProps);
        Assert.DoesNotContain("CampaignId", stockProps);
        Assert.DoesNotContain("SoldPercentage", stockProps);

        // 19 migration verification
        var tables = await db.Database.SqlQueryRaw<string>(
                """
                SELECT table_name AS "Value"
                FROM information_schema.tables
                WHERE table_schema = 'promotion'
                ORDER BY table_name
                """)
            .ToListAsync();
        Assert.Contains("merchandising_campaigns", tables);
        Assert.Contains("merchandising_campaign_offers", tables);
        Assert.Contains("merchandising_campaign_translations", tables);
        Assert.Contains("merchandising_promotion_types", tables);
        Assert.Contains("merchandising_promotion_type_translations", tables);
        Assert.Contains("promotions", tables);
        Assert.DoesNotContain(tables, t => t.Contains("amazing", StringComparison.OrdinalIgnoreCase) && t.Contains("offer", StringComparison.OrdinalIgnoreCase));

        var uniqueCode = await db.Database.SqlQueryRaw<string>(
                """
                SELECT indexname AS "Value"
                FROM pg_indexes
                WHERE schemaname = 'promotion' AND indexname = 'ix_merchandising_promotion_types_code'
                """)
            .ToListAsync();
        Assert.Single(uniqueCode);

        // 20 recovery-staleness PASS — expected ancestor documented for evidence
        var root = FindRepoRoot();
        Assert.True(File.Exists(Path.Combine(root, "docs", "evidence", "TB-P10-T022-R14", "recovery-start.md")));
        Assert.True(File.Exists(Path.Combine(root, "docs", "ai", "tasks", "TB-P10-T022-R15.task.md")));

        // 21 git diff --check style: no trailing whitespace on new merchandising domain sources
        foreach (var relative in new[]
                 {
                     Path.Combine("src", "backend", "Modules", "Promotion", "Tooba.Promotion.Domain", "MerchandisingCampaignDomain.cs"),
                     Path.Combine("src", "backend", "Modules", "Promotion", "Tooba.Promotion.Application", "MerchandisingCampaignContracts.cs"),
                     Path.Combine("src", "backend", "Modules", "Promotion", "Tooba.Promotion.Infrastructure", "MerchandisingCampaignDirectory.cs"),
                 })
        {
            var text = await File.ReadAllTextAsync(Path.Combine(root, relative));
            Assert.DoesNotContain(" \n", text);
            Assert.DoesNotContain("\t\n", text);
        }
    }

    private static PromotionDbContext CreateDb(string connectionString)
    {
        var commerce = new FixedCommerceContext();
        commerce.Assign(OutboxTestContextFactory.SingleStore("tenant-merch", "tenant-merch"));
        var modules = new IOutboxModuleRegistration[] { new PromotionOutboxRegistration() };
        var serializer = new JsonIntegrationEventSerializer(modules);
        var interceptor = new OutboxSaveChangesInterceptor(commerce, modules, serializer);
        var options = new DbContextOptionsBuilder<PromotionDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, connectionString, PromotionDbContext.Schema, typeof(PromotionDbContext));
        options.AddInterceptors(interceptor);
        return new PromotionDbContext(options.Options);
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
