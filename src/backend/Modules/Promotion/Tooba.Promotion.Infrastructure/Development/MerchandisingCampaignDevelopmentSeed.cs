using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Contracts;
using Tooba.Catalog.Contracts.Ports;
using Tooba.Inventory.Contracts.Availability;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;
using Tooba.Party.Contracts.Ports;
using Tooba.Pricing.Contracts;
using Tooba.Pricing.Contracts.Ports;
using Tooba.Promotion.Application.Promotions.Ports;
using Tooba.Promotion.Application.Checkout;
using Tooba.Promotion.Application.Merchandising.Ports;
using Tooba.Promotion.Contracts.Merchandising;
using Tooba.Promotion.Domain.Aggregates;
using Tooba.Promotion.Domain.ValueObjects;
using Tooba.Promotion.Domain.Events;
using Tooba.Promotion.Domain.Merchandising;

namespace Tooba.Promotion.Infrastructure.Development;

/// <summary>
/// دانهٔ idempotent کمپین‌های AMAZING برای Development؛ Production را لمس نمی‌کند.
/// پنجره‌ها نسبت به UtcNow تازه می‌شوند تا سناریوهای active/future/expired پایدار بمانند.
/// دسترسی به Inventory فقط از طریق قرارداد <see cref="IInventoryDevelopmentSeedGateway"/> است.
/// </summary>
public static class MerchandisingCampaignDevelopmentSeed
{
    internal const string OosSellerSku = "DEV-SEED-OOS";
    internal const string OosLocationCode = "WH-DEV-OOS";
    internal const string MarkerActivePrimary = "[DEV-SEED] Amazing Active Primary";
    internal const string MarkerActiveLoser = "[DEV-SEED] Amazing Active Loser";
    internal const string MarkerFuture = "[DEV-SEED] Amazing Future Teasing";
    internal const string MarkerExpired = "[DEV-SEED] Amazing Expired";
    internal const string MarkerDraft = "[DEV-SEED] Amazing Draft";

    /// <summary>سقف Productهای کمپین فعال اصلی؛ هر Product با Offer ای که Storefront رندر می‌کند عضو می‌شود.</summary>
    private const int MaxPrimaryMemberProducts = 8;

    /// <summary>
    /// کسرهای ثابت Base برای هر محصول تا ریل تخفیف یکنواخت نباشد؛ ثابت‌اند تا دانه قطعی بماند.
    /// </summary>
    private static readonly decimal[] PromoFractions = [0.70m, 0.75m, 0.65m, 0.80m, 0.60m, 0.72m, 0.68m, 0.78m];

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
        var inventoryQuery = provider.GetRequiredService<IInventoryQueryGateway>();
        var inventory = provider.GetRequiredService<IInventoryDevelopmentSeedGateway>();
        var parties = provider.GetRequiredService<IPartyDevelopmentDirectory>();
        var prices = provider.GetRequiredService<IPriceDirectory>();
        var priceQuery = provider.GetRequiredService<IPriceQueryGateway>();
        var clock = provider.GetRequiredService<IClock>();
        var now = clock.UtcNow;

        // Prefer offers whose Catalog Product is Published so Storefront ProductCards can project promo prices.
        var catalogLookup = provider.GetRequiredService<ICatalogVariantLookup>();
        var offerRead = provider.GetRequiredService<ICatalogOfferReadGateway>();
        var publishedVariantSet = (await catalogLookup.GetPublishedVariantIdsAsync(cancellationToken)).ToHashSet();
        var allActive = await offerQueries.ListActiveOffersAsync(cancellationToken);
        var candidates = allActive
            .Where(x => x.SellerSku != OosSellerSku && publishedVariantSet.Contains(x.CatalogVariantId))
            .ToList();
        if (candidates.Count < 4)
        {
            // Fallback: any active offers if published set is thin.
            candidates = allActive.Where(x => x.SellerSku != OosSellerSku).ToList();
        }

        // اعضا همان Offerهایی هستند که Storefront واقعاً به‌عنوان Primary کارت رندر می‌کند؛
        // اگر قیمت کمپین روی Offer دیگری از همان Product بنشیند، هیچ کارتی تخفیف نشان نمی‌دهد.
        var primaryOffers = await ResolveStorefrontPrimaryOffersAsync(
            offerRead,
            priceQuery,
            inventoryQuery,
            provider.GetRequiredService<IPrimaryOfferSelectionPolicy>(),
            candidates,
            MaxPrimaryMemberProducts,
            now,
            cancellationToken);
        var activeOffers = primaryOffers.Select(x => x.OfferId).ToList();

        var oosOfferId = await EnsureOosOfferAsync(
            offerSeeds,
            inventoryQuery,
            inventory,
            parties,
            cancellationToken);
        var primaryMembers = activeOffers.ToList();
        if (oosOfferId is not null)
        {
            primaryMembers.Add(oosOfferId.Value);
        }

        await UpsertCampaignAsync(
            dir,
            MerchandisingDevelopmentIds.ActivePrimaryId,
            type.Id,
            MerchandisingDevelopmentIds.StoreAlphaId,
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
            MerchandisingDevelopmentIds.ActiveLoserId,
            type.Id,
            MerchandisingDevelopmentIds.StoreAlphaId,
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
            MerchandisingDevelopmentIds.FutureId,
            type.Id,
            MerchandisingDevelopmentIds.StoreAlphaId,
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
            MerchandisingDevelopmentIds.ExpiredId,
            type.Id,
            MerchandisingDevelopmentIds.StoreAlphaId,
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
            MerchandisingDevelopmentIds.DraftId,
            type.Id,
            MerchandisingDevelopmentIds.StoreAlphaId,
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
        // Active primary: هر عضو قیمت کمپین کمتر از Base می‌گیرد جز آخرین عضو که عمداً
        // base-only می‌ماند تا شاخهٔ fallback (فقط قیمت پایه) در دانهٔ Development پوشش داده شود.
        await EnsureCampaignPromoPricesAsync(
            prices,
            priceQuery,
            MerchandisingDevelopmentIds.ActivePrimaryId,
            BuildPromoTargets(primaryOffers, skipLast: true),
            validFrom: now.AddHours(-2),
            validTo: now.AddDays(7),
            cancellationToken);
        // Future: promo rows exist but must not apply until StartAt.
        await EnsureCampaignPromoPricesAsync(
            prices,
            priceQuery,
            MerchandisingDevelopmentIds.FutureId,
            BuildPromoTargets(primaryOffers.Take(1).ToList(), skipLast: false, fractionOverride: 0.5m),
            validFrom: now.AddDays(1),
            validTo: now.AddDays(2),
            cancellationToken);
    }

    /// <summary>
    /// هدف‌های نوشتن قیمت کمپین را از Offerهای Primary می‌سازد؛ مبلغ همیشه اکیداً کمتر از Base است
    /// تا <c>IsAmazingRailEligible</c> و overlay فروشگاه بتوانند تخفیف را بپذیرند.
    /// </summary>
    private static IReadOnlyList<CampaignPromoTarget> BuildPromoTargets(
        IReadOnlyList<StorefrontPrimaryOffer> offers,
        bool skipLast,
        decimal? fractionOverride = null)
    {
        var take = skipLast && offers.Count > 1 ? offers.Count - 1 : offers.Count;
        var targets = new List<CampaignPromoTarget>(take);
        for (var i = 0; i < take; i++)
        {
            var offer = offers[i];
            var fraction = fractionOverride ?? PromoFractions[i % PromoFractions.Length];
            var amount = decimal.Round(offer.BaseAmount * fraction, 0, MidpointRounding.AwayFromZero);
            if (amount <= 0 || amount >= offer.BaseAmount)
            {
                amount = Math.Max(1, offer.BaseAmount - 1);
            }

            targets.Add(new CampaignPromoTarget(
                offer.OfferId,
                amount,
                offer.Market,
                offer.Channel,
                offer.Currency));
        }

        return targets;
    }

    /// <summary>
    /// برای هر Product منتشرشده همان Offer را برمی‌گرداند که Storefront به‌عنوان Primary انتخاب می‌کند.
    /// ترتیب انتخاب دوباره پیاده‌سازی نمی‌شود؛ همین کاندیداها به درز مالک Offer یعنی
    /// <see cref="IPrimaryOfferSelectionPolicy"/> داده می‌شوند تا فروشگاه و دانه یک حکم واحد بگیرند.
    /// Base برگشتی همان ردیفی است که کارت فروشگاه نمایش می‌دهد، پس Market/Currency/Channel
    /// قیمت کمپین هم با آن یکی می‌شود.
    /// </summary>
    internal static async Task<IReadOnlyList<StorefrontPrimaryOffer>> ResolveStorefrontPrimaryOffersAsync(
        ICatalogOfferReadGateway offerRead,
        IPriceQueryGateway priceQuery,
        IInventoryQueryGateway inventoryQuery,
        IPrimaryOfferSelectionPolicy primaryOffers,
        IReadOnlyList<OfferReference> candidates,
        int maxProducts,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (candidates.Count == 0)
        {
            return [];
        }

        var presentations = await offerRead.GetOfferPresentationsAsync(
            candidates.Select(x => x.CatalogVariantId).Distinct().ToArray(),
            cancellationToken);
        var selected = new List<StorefrontPrimaryOffer>();
        var byProduct = candidates
            .Where(x => presentations.ContainsKey(x.CatalogVariantId))
            .GroupBy(x => presentations[x.CatalogVariantId].ProductId)
            .OrderBy(group => group.Key)
            .Take(maxProducts);

        foreach (var group in byProduct)
        {
            var offerIds = group.Select(x => x.OfferId).Distinct().ToArray();
            var prices = await priceQuery.ListByOfferIdsAsync(offerIds, cancellationToken);
            var positions = await inventoryQuery.ListPositionsByOfferIdsAsync(offerIds, cancellationToken);

            // همان زمینهٔ فروشگاه: فقط Offerهایی که Base معتبر دارند کاندیدا می‌شوند و
            // Available همان جمع موقعیت‌های انبار است؛ انتخاب به‌عهدهٔ سیاست مالک Offer است.
            var priceByOffer = new Dictionary<Guid, AuthoredPriceSnapshot>();
            var selectionCandidates = new List<OfferSelectionCandidate>();
            foreach (var offer in group)
            {
                var candidateBase = prices
                    .Where(p => p.OfferId == offer.OfferId
                        && p.Status == "Active"
                        && p.QualifierKind == "Base"
                        && p.ValidFrom <= now
                        && (p.ValidTo is null || p.ValidTo >= now))
                    .OrderBy(p => p.Amount)
                    .FirstOrDefault();
                if (candidateBase is null)
                {
                    continue;
                }

                priceByOffer[offer.OfferId] = candidateBase;
                selectionCandidates.Add(new OfferSelectionCandidate(
                    offer.OfferId,
                    offer.CatalogVariantId,
                    offer.SellerPartyId,
                    candidateBase.Amount,
                    positions.Where(p => p.OfferId == offer.OfferId).Sum(p => p.Available)));
            }

            var primary = primaryOffers.Resolve(selectionCandidates);
            if (primary is null)
            {
                continue;
            }

            var basePrice = priceByOffer[primary.OfferId];
            selected.Add(new StorefrontPrimaryOffer(
                primary.OfferId,
                basePrice.Amount,
                basePrice.Market,
                basePrice.Channel,
                basePrice.Currency));
        }

        return selected;
    }

    /// <summary>
    /// قیمت کمپین را upsert می‌کند. ردیف فعالی که کل پنجرهٔ درخواستی را پوشش دهد دست‌نخورده
    /// رد می‌شود؛ ردیفی که فقط بخشی از پنجره را می‌پوشاند اول Expire می‌شود تا ردیف تازه ساخته شود.
    /// بدون این بازآفرینی، دانهٔ idempotent بعد از کوتاه‌شدن پنجرهٔ قبلی هیچ‌وقت تخفیف را کامل نمی‌کند.
    /// </summary>
    internal static async Task EnsureCampaignPromoPricesAsync(
        IPriceDirectory prices,
        IPriceQueryGateway priceQuery,
        Guid campaignId,
        IReadOnlyList<CampaignPromoTarget> targets,
        DateTimeOffset validFrom,
        DateTimeOffset? validTo,
        CancellationToken cancellationToken)
    {
        if (targets.Count == 0)
        {
            return;
        }

        var key = campaignId.ToString("D");
        var rows = await priceQuery.ListByOfferIdsAsync(
            targets.Select(x => x.OfferId).Distinct().ToArray(),
            cancellationToken);
        var campaignRows = rows
            .Where(x => x.QualifierKind == "MerchandisingCampaign"
                && string.Equals(x.QualifierKey, key, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var target in targets)
        {
            var existing = campaignRows.Where(x => x.OfferId == target.OfferId).ToList();
            var covered = existing.Any(x => x.Status == "Active"
                && CoversRequiredWindow(x.ValidFrom, x.ValidTo, validFrom, validTo));
            if (covered)
            {
                continue;
            }

            foreach (var stale in existing.Where(x => x.Status == "Active"))
            {
                await prices.ExpireAsync(stale.PriceId, cancellationToken);
            }

            var created = await prices.CreateCampaignPriceAsync(
                target.OfferId,
                campaignId,
                target.Market,
                target.Channel,
                target.Amount,
                target.Currency,
                validFrom,
                validTo,
                cancellationToken);
            await prices.ActivateAsync(created.PriceId, cancellationToken);
        }
    }

    /// <summary>
    /// آیا بازهٔ ردیف موجود، تمام پنجرهٔ درخواستی را پوشش می‌دهد؟ بازهٔ قیمت نوشته‌شده در Pricing
    /// نیم‌باز <c>[ValidFrom, ValidTo)</c> است — شروع شامل، پایان انحصاری — مطابق
    /// <c>AuthoredPrice.IsEffectiveAt</c>. پس پوشش کامل یعنی شروع ردیف نه‌دیرتر از شروع درخواست
    /// و پایان ردیف نه‌زودتر از پایان درخواست؛ پنجرهٔ بدون پایان فقط با ردیف بدون پایان پوشش می‌یابد.
    /// </summary>
    internal static bool CoversRequiredWindow(
        DateTimeOffset rowValidFrom,
        DateTimeOffset? rowValidTo,
        DateTimeOffset requiredFrom,
        DateTimeOffset? requiredTo) =>
        rowValidFrom <= requiredFrom
        && (requiredTo is null
            ? rowValidTo is null
            : rowValidTo is null || rowValidTo >= requiredTo);

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
        IInventoryDevelopmentSeedGateway inventory,
        IPartyDevelopmentDirectory parties,
        CancellationToken cancellationToken)
    {
        const string oosSellerName = "DEV-SEED OOS Seller";
        var matches = await parties.SearchIdsByDisplayNameAsync(oosSellerName, 5, cancellationToken);
        var sellerPartyId = matches.FirstOrDefault();
        if (sellerPartyId == Guid.Empty)
        {
            var createdSeller = await parties.EnsureDevelopmentOrganizationAsync(
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
        IInventoryDevelopmentSeedGateway inventory,
        CancellationToken cancellationToken)
    {
        var locationId = await inventoryQuery.FindFirstActiveLocationIdAsync(cancellationToken) ?? Guid.Empty;
        if (locationId == Guid.Empty)
        {
            var ensured = await inventory.EnsureDevelopmentLocationAsync(
                OosLocationCode,
                "Dev OOS bin",
                cancellationToken);
            if (ensured.IsFailure)
            {
                throw new SemanticException(ensured.FirstError);
            }

            locationId = ensured.Value;
        }

        var drained = await inventory.DrainDevelopmentStockAsync(
            new SeedDevelopmentStockDrain(offerId, locationId, "dev-seed-oos-drain"),
            cancellationToken);
        if (drained.IsFailure)
        {
            throw new SemanticException(drained.FirstError);
        }
    }

    /// <summary>Offer ای که Storefront به‌عنوان Primary کارت Product انتخاب می‌کند، همراه با Base نمایش‌داده‌شده.</summary>
    internal sealed record StorefrontPrimaryOffer(
        Guid OfferId,
        decimal BaseAmount,
        string Market,
        SalesChannel Channel,
        string Currency);

    /// <summary>مبلغ و قیمت‌گذاری نوشته‌شده برای یک Offer در کمپین.</summary>
    internal sealed record CampaignPromoTarget(
        Guid OfferId,
        decimal Amount,
        string Market,
        SalesChannel Channel,
        string Currency);
}
