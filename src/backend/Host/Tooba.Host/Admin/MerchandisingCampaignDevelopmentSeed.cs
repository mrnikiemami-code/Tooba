using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Inventory.Application.Ports;
using Tooba.Inventory.Application.Checkout;
using Tooba.Inventory.Application.Orders;
using Tooba.Inventory.Contracts.Returns;
using Tooba.Inventory.Contracts.Availability;
using Tooba.Inventory.Contracts.Checkout;
using Tooba.Inventory.Contracts.Errors;
using Tooba.Inventory.Contracts.Orders;
using Tooba.Inventory.Contracts.Seller;
using Tooba.Inventory.Domain.Aggregates;
using Tooba.Inventory.Domain.ValueObjects;
using Tooba.Inventory.Domain.Events;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;
using Tooba.Party.Application;
using Tooba.Party.Infrastructure.Persistence;
using Tooba.Pricing.Application;
using Tooba.Pricing.Contracts;
using Tooba.Promotion.Application.Ports;
using Tooba.Promotion.Application.Checkout;
using Tooba.Promotion.Application.Merchandising;
using Tooba.Promotion.Domain.Aggregates;
using Tooba.Promotion.Domain.ValueObjects;
using Tooba.Promotion.Domain.Events;
using Tooba.Promotion.Domain.Merchandising;

namespace Tooba.Host.Admin;

/// <summary>
/// دانهٔ idempotent کمپین‌های AMAZING برای Development؛ Production را لمس نمی‌کند.
/// پنجره‌ها نسبت به UtcNow تازه می‌شوند تا سناریوهای active/future/expired پایدار بمانند.
/// </summary>
internal static class MerchandisingCampaignDevelopmentSeed
{
    /// <summary>StoreId پایدار برای tenant SingleStore store-alpha داخل DB tenant.</summary>
    public static readonly Guid StoreAlphaId = Guid.Parse("aaaaaaaa-aaaa-7aaa-8aaa-aaaaaaaaaaa1");

    /// <summary>کمپین فعال اولویت بالا.</summary>
    public static readonly Guid ActivePrimaryId = Guid.Parse("019a16a0-0001-7000-8000-000000000001");

    /// <summary>کمپین فعال بازندهٔ اولویت.</summary>
    public static readonly Guid ActiveLoserId = Guid.Parse("019a16a0-0002-7000-8000-000000000002");

    /// <summary>کمپین آینده / teasing.</summary>
    public static readonly Guid FutureId = Guid.Parse("019a16a0-0003-7000-8000-000000000003");

    /// <summary>کمپین منقضی.</summary>
    public static readonly Guid ExpiredId = Guid.Parse("019a16a0-0004-7000-8000-000000000004");

    /// <summary>کمپین پیش‌نویس.</summary>
    public static readonly Guid DraftId = Guid.Parse("019a16a0-0005-7000-8000-000000000005");

    internal const string OosSellerSku = "DEV-SEED-OOS";
    internal const string MarkerActivePrimary = "[DEV-SEED] Amazing Active Primary";
    internal const string MarkerActiveLoser = "[DEV-SEED] Amazing Active Loser";
    internal const string MarkerFuture = "[DEV-SEED] Amazing Future Teasing";
    internal const string MarkerExpired = "[DEV-SEED] Amazing Expired";
    internal const string MarkerDraft = "[DEV-SEED] Amazing Draft";

    /// <summary>
    /// کمپین‌های AMAZING را فقط در Development upsert می‌کند.
    /// </summary>
    public static async Task EnsureAsync(IServiceProvider provider, CancellationToken cancellationToken)
    {
        var environment = provider.GetRequiredService<IHostEnvironment>();
        if (!environment.IsDevelopment())
        {
            return;
        }

        var dir = provider.GetRequiredService<IMerchandisingCampaignDirectory>();
        var type = await dir.EnsureAmazingTypeSeededAsync(cancellationToken);
        var offerQueries = provider.GetRequiredService<IOfferQueryGateway>();
        var offerSeeds = provider.GetRequiredService<IOfferDevelopmentSeedGateway>();
        var catalogDb = provider.GetRequiredService<CatalogDbContext>();
        var inventoryQuery = provider.GetRequiredService<IInventoryQueryGateway>();
        var inventory = provider.GetRequiredService<IInventoryDirectory>();
        var parties = provider.GetRequiredService<IPartyDirectory>();
        var partyDb = provider.GetRequiredService<PartyDbContext>();
        var prices = provider.GetRequiredService<IPriceDirectory>();
        var priceQuery = provider.GetRequiredService<IPriceQueryGateway>();
        var now = DateTimeOffset.UtcNow;

        // Prefer offers whose Catalog Product is Published so Storefront ProductCards can project promo prices.
        var publishedVariantIds = await catalogDb.Variants.AsNoTracking()
            .Where(v => catalogDb.Products.Any(p =>
                p.ProductId == v.ProductId && p.Status == CatalogPublicationStatus.Published))
            .Select(v => v.VariantId)
            .ToListAsync(cancellationToken);
        var publishedVariantSet = publishedVariantIds.ToHashSet();
        var allActive = await offerQueries.ListActiveOffersAsync(cancellationToken);
        var activeOffers = allActive
            .Where(x => x.SellerSku != OosSellerSku && publishedVariantSet.Contains(x.CatalogVariantId))
            .OrderBy(x => x.OfferId)
            .Take(8)
            .Select(x => x.OfferId)
            .ToList();
        if (activeOffers.Count < 4)
        {
            // Fallback: any active offers if published set is thin.
            activeOffers = allActive
                .Where(x => x.SellerSku != OosSellerSku)
                .OrderBy(x => x.OfferId)
                .Take(8)
                .Select(x => x.OfferId)
                .ToList();
        }

        var oosOfferId = await EnsureOosOfferAsync(
            offerSeeds,
            inventoryQuery,
            inventory,
            parties,
            partyDb,
            cancellationToken);
        var primaryMembers = activeOffers.ToList();
        if (oosOfferId is not null)
        {
            primaryMembers.Add(oosOfferId.Value);
        }

        await UpsertCampaignAsync(
            dir,
            ActivePrimaryId,
            type.Id,
            StoreAlphaId,
            startAt: now.AddHours(-2),
            endAt: now.AddDays(7),
            priority: 100,
            MerchandisingCampaignLifecycleStatus.Published,
            MarkerActivePrimary,
            "پیشنهاد شگفت‌انگیز فعال",
            "Amazing Active Primary",
            primaryMembers,
            cancellationToken);

        await UpsertCampaignAsync(
            dir,
            ActiveLoserId,
            type.Id,
            StoreAlphaId,
            startAt: now.AddHours(-1),
            endAt: now.AddDays(3),
            priority: 10,
            MerchandisingCampaignLifecycleStatus.Published,
            MarkerActiveLoser,
            "پیشنهاد شگفت‌انگیز بازنده",
            "Amazing Active Loser",
            activeOffers.Take(2).ToList(),
            cancellationToken);

        await UpsertCampaignAsync(
            dir,
            FutureId,
            type.Id,
            StoreAlphaId,
            startAt: now.AddDays(1),
            endAt: now.AddDays(2),
            priority: 80,
            MerchandisingCampaignLifecycleStatus.Published,
            MarkerFuture,
            "فردای شگفت‌انگیز",
            "Amazing Future Teasing",
            activeOffers.Take(3).ToList(),
            cancellationToken);

        await UpsertCampaignAsync(
            dir,
            ExpiredId,
            type.Id,
            StoreAlphaId,
            startAt: now.AddDays(-3),
            endAt: now.AddDays(-1),
            priority: 90,
            MerchandisingCampaignLifecycleStatus.Published,
            MarkerExpired,
            "شگفت‌انگیز منقضی",
            "Amazing Expired",
            activeOffers.Take(1).ToList(),
            cancellationToken);

        await UpsertCampaignAsync(
            dir,
            DraftId,
            type.Id,
            StoreAlphaId,
            startAt: now.AddHours(-1),
            endAt: now.AddDays(1),
            priority: 50,
            MerchandisingCampaignLifecycleStatus.Draft,
            MarkerDraft,
            "شگفت‌انگیز پیش‌نویس",
            "Amazing Draft",
            activeOffers.Take(1).ToList(),
            cancellationToken);

        // R18: campaign-scoped AuthoredPrice seeds (not PromoAmount scalar).
        // Active primary: first 3 members get promo < base; 4th (if present) keeps base-only fallback.
        await EnsureCampaignPromoPricesAsync(
            prices,
            priceQuery,
            ActivePrimaryId,
            activeOffers.Take(3).ToList(),
            fractionOfBase: 0.7m,
            validFrom: now.AddHours(-2),
            validTo: now.AddDays(7),
            cancellationToken);
        // Future: promo rows exist but must not apply until StartAt.
        await EnsureCampaignPromoPricesAsync(
            prices,
            priceQuery,
            FutureId,
            activeOffers.Take(1).ToList(),
            fractionOfBase: 0.5m,
            validFrom: now.AddDays(1),
            validTo: now.AddDays(2),
            cancellationToken);
    }

    /// <summary>
    /// برای هر Offer، در صورت نبود قیمت کمپین، مبلغی کمتر از پایه می‌نویسد و فعال می‌کند.
    /// </summary>
    private static async Task EnsureCampaignPromoPricesAsync(
        IPriceDirectory prices,
        IPriceQueryGateway priceQuery,
        Guid campaignId,
        IReadOnlyList<Guid> offerIds,
        decimal fractionOfBase,
        DateTimeOffset validFrom,
        DateTimeOffset? validTo,
        CancellationToken cancellationToken)
    {
        if (offerIds.Count == 0)
        {
            return;
        }

        var key = campaignId.ToString("D");
        var existing = await priceQuery.ListActiveCampaignOfferIdsAsync(offerIds, key, cancellationToken);
        var have = existing.ToHashSet();

        foreach (var offerId in offerIds)
        {
            if (have.Contains(offerId))
            {
                continue;
            }

            var basePrice = await priceQuery.FindLatestActiveBaseAsync(offerId, "IR", "IRR", cancellationToken);
            if (basePrice is null || basePrice.Amount <= 0)
            {
                continue;
            }

            var promoAmount = decimal.Round(basePrice.Amount * fractionOfBase, 0, MidpointRounding.AwayFromZero);
            if (promoAmount <= 0 || promoAmount >= basePrice.Amount)
            {
                promoAmount = Math.Max(1, basePrice.Amount - 1);
            }

            var created = await prices.CreateCampaignPriceAsync(
                offerId,
                campaignId,
                basePrice.Market,
                basePrice.Channel,
                promoAmount,
                basePrice.Currency,
                validFrom,
                validTo,
                cancellationToken);
            await prices.ActivateAsync(created.PriceId, cancellationToken);
        }
    }

    private static async Task UpsertCampaignAsync(
        IMerchandisingCampaignDirectory dir,
        Guid campaignId,
        Guid typeId,
        Guid storeId,
        DateTimeOffset startAt,
        DateTimeOffset? endAt,
        int priority,
        MerchandisingCampaignLifecycleStatus lifecycle,
        string markerTitleEn,
        string titleFa,
        string titleEn,
        IReadOnlyList<Guid> memberOfferIds,
        CancellationToken cancellationToken)
    {
        await dir.UpsertSeedCampaignAsync(
            campaignId,
            typeId,
            storeId,
            startAt,
            endAt,
            priority,
            lifecycle,
            cancellationToken);
        await dir.UpsertCampaignTranslationAsync(
            campaignId,
            "fa-IR",
            $"{markerTitleEn} | {titleFa}",
            subtitle: null,
            badgeText: "Amazing",
            cancellationToken);
        await dir.UpsertCampaignTranslationAsync(
            campaignId,
            "en-US",
            markerTitleEn,
            subtitle: titleEn,
            badgeText: "Amazing",
            cancellationToken);
        if (memberOfferIds.Count > 0)
        {
            await dir.SyncSeedMembersAsync(campaignId, storeId, memberOfferIds, cancellationToken);
        }
    }

    private static async Task<Guid?> EnsureOosOfferAsync(
        IOfferDevelopmentSeedGateway offerSeeds,
        IInventoryQueryGateway inventoryQuery,
        IInventoryDirectory inventory,
        IPartyDirectory parties,
        PartyDbContext partyDb,
        CancellationToken cancellationToken)
    {
        const string oosSellerName = "DEV-SEED OOS Seller";
        var sellerPartyId = await partyDb.Parties.AsNoTracking()
            .Where(x => x.DisplayName == oosSellerName)
            .Select(x => x.PartyId)
            .FirstOrDefaultAsync(cancellationToken);
        if (sellerPartyId == Guid.Empty)
        {
            var createdSeller = await parties.CreateOrganizationAsync(
                oosSellerName,
                "DEV-SEED OOS Seller Legal",
                cancellationToken);
            sellerPartyId = createdSeller.PartyId;
        }

        var offerId = await offerSeeds.EnsureActiveCloneFromAnyActiveAsync(
            OosSellerSku,
            sellerPartyId,
            cancellationToken);
        if (offerId is null)
        {
            return null;
        }

        await EnsureZeroStockAsync(offerId.Value, inventoryQuery, inventory, cancellationToken);
        return offerId;
    }

    private static async Task EnsureZeroStockAsync(
        Guid offerId,
        IInventoryQueryGateway inventoryQuery,
        IInventoryDirectory inventory,
        CancellationToken cancellationToken)
    {
        var locationId = await inventoryQuery.FindFirstActiveLocationIdAsync(cancellationToken) ?? Guid.Empty;
        if (locationId == Guid.Empty)
        {
            locationId = await inventory.CreateLocationAsync("WH-DEV-OOS", "Dev OOS bin", cancellationToken);
        }

        var stockItemId = await inventory.OpenPositionAsync(offerId, locationId, cancellationToken);
        var position = await inventoryQuery.FindPositionByStockItemIdAsync(stockItemId, cancellationToken)
            ?? throw new InvalidOperationException("dev-seed-oos position missing");
        var available = position.Available;
        if (available > 0)
        {
            await inventory.AdjustAsync(
                stockItemId,
                StockAdjustmentKind.Decrease,
                available,
                "dev-seed-oos-drain",
                "dev-seed-oos-drain",
                cancellationToken);
        }
    }
}
