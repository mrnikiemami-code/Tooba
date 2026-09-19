using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Inventory.Application;
using Tooba.Offer.Domain;
using Tooba.Offer.Infrastructure.Persistence;
using Tooba.Party.Infrastructure.Persistence;
using Tooba.Pricing.Application;
using Tooba.Promotion.Application;
using Tooba.Promotion.Domain;

namespace Tooba.Host.Admin;

#pragma warning disable CS1591

public sealed record AdminMerchCampaignListItem(
    Guid CampaignId,
    string Title,
    string PromotionTypeDisplayName,
    string LifecycleStatus,
    string RuntimeLabel,
    DateTimeOffset StartAt,
    DateTimeOffset? EndAt,
    int Priority,
    int MemberCount,
    DateTimeOffset UpdatedAt);

public sealed record AdminMerchCampaignListResponse(IReadOnlyList<AdminMerchCampaignListItem> Items, int Total);

public sealed record AdminMerchCampaignTypeOption(Guid PromotionTypeId, string DisplayName);

public sealed record AdminMerchCampaignTypesResponse(IReadOnlyList<AdminMerchCampaignTypeOption> Items);

public sealed record AdminMerchCampaignTranslationDto(
    string Locale,
    string Title,
    string? Subtitle,
    string? BadgeText);

public sealed record AdminMerchCampaignMemberDto(
    Guid SellerOfferId,
    int SortOrder,
    string ProductTitle,
    string SellerDisplayName,
    decimal BaseAmount,
    decimal? CampaignAmount,
    string Currency,
    decimal AvailableUnits,
    bool InStock);

public sealed record AdminMerchCampaignDetail(
    Guid CampaignId,
    Guid PromotionTypeId,
    string PromotionTypeDisplayName,
    string LifecycleStatus,
    string RuntimeLabel,
    DateTimeOffset StartAt,
    DateTimeOffset? EndAt,
    int Priority,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<AdminMerchCampaignTranslationDto> Translations,
    IReadOnlyList<AdminMerchCampaignMemberDto> Members);

public sealed record AdminMerchCampaignWriteRequest(
    Guid PromotionTypeId,
    DateTimeOffset StartAt,
    DateTimeOffset? EndAt,
    int Priority,
    IReadOnlyList<AdminMerchCampaignTranslationDto> Translations);

public sealed record AdminMerchCampaignUpdateRequest(
    DateTimeOffset StartAt,
    DateTimeOffset? EndAt,
    int Priority,
    IReadOnlyList<AdminMerchCampaignTranslationDto> Translations);

public sealed record AdminMerchAddMemberRequest(Guid SellerOfferId);

public sealed record AdminMerchReorderMembersRequest(IReadOnlyList<Guid> OrderedSellerOfferIds);

public sealed record AdminMerchMemberPriceRequest(
    decimal Amount,
    string? Currency,
    string? Market,
    string? Channel);

public sealed record AdminMerchOfferCandidate(
    Guid SellerOfferId,
    string ProductTitle,
    string SellerDisplayName,
    decimal BaseAmount,
    string Currency,
    decimal AvailableUnits,
    bool InStock);

public sealed record AdminMerchOfferCandidateResponse(IReadOnlyList<AdminMerchOfferCandidate> Items, int Total);

/// <summary>
/// ترکیب Admin کمپین مرچندایزینگ: دایرکتوری Promotion + Pricing + نمایش Offer/Catalog.
/// </summary>
public sealed class MerchandisingCampaignAdminComposer
{
    private readonly IMerchandisingCampaignDirectory _campaigns;
    private readonly IPriceDirectory _prices;
    private readonly IPriceLookupGateway _priceLookup;
    private readonly IInventoryAvailabilityGateway _availability;
    private readonly OfferDbContext _offers;
    private readonly CatalogDbContext _catalog;
    private readonly PartyDbContext _parties;
    private readonly ICurrentCommerceContext _commerce;

    public MerchandisingCampaignAdminComposer(
        IMerchandisingCampaignDirectory campaigns,
        IPriceDirectory prices,
        IPriceLookupGateway priceLookup,
        IInventoryAvailabilityGateway availability,
        OfferDbContext offers,
        CatalogDbContext catalog,
        PartyDbContext parties,
        ICurrentCommerceContext commerce)
    {
        _campaigns = campaigns;
        _prices = prices;
        _priceLookup = priceLookup;
        _availability = availability;
        _offers = offers;
        _catalog = catalog;
        _parties = parties;
        _commerce = commerce;
    }

    public Guid ResolveStoreId()
    {
        var tenantId = _commerce.Current?.Tenant?.TenantId.Value;
        if (string.Equals(tenantId, "store-alpha", StringComparison.OrdinalIgnoreCase))
        {
            return MerchandisingCampaignDevelopmentSeed.StoreAlphaId;
        }

        if (Guid.TryParse(tenantId, out var parsed) && parsed != Guid.Empty)
        {
            return parsed;
        }

        return MerchandisingCampaignDevelopmentSeed.StoreAlphaId;
    }

    public async Task<AdminMerchCampaignListResponse> ListAsync(
        string? search,
        string? lifecycle,
        Guid? promotionTypeId,
        string? runtimeWindow,
        int skip,
        int take,
        string? locale,
        CancellationToken cancellationToken)
    {
        var storeId = ResolveStoreId();
        var now = DateTimeOffset.UtcNow;
        MerchandisingCampaignLifecycleStatus? life = null;
        if (!string.IsNullOrWhiteSpace(lifecycle)
            && Enum.TryParse<MerchandisingCampaignLifecycleStatus>(lifecycle, true, out var parsedLife))
        {
            life = parsedLife;
        }

        var (rows, total) = await _campaigns.ListCampaignsAsync(
            storeId,
            search,
            life,
            promotionTypeId,
            runtimeWindow,
            now,
            locale ?? "fa-IR",
            skip,
            take,
            cancellationToken);
        var items = rows.Select(r => new AdminMerchCampaignListItem(
            r.Id,
            r.Title ?? "بدون عنوان",
            r.PromotionTypeDisplayName,
            r.LifecycleStatus.ToString(),
            r.RuntimeLabel,
            r.StartAt,
            r.EndAt,
            r.Priority,
            r.MemberCount,
            r.UpdatedAt)).ToList();
        return new AdminMerchCampaignListResponse(items, total);
    }

    public async Task<AdminMerchCampaignTypesResponse> ListTypesAsync(string? locale, CancellationToken cancellationToken)
    {
        var options = await _campaigns.ListPromotionTypeOptionsAsync(locale ?? "fa-IR", cancellationToken);
        return new AdminMerchCampaignTypesResponse(
            options.Select(o => new AdminMerchCampaignTypeOption(o.Id, o.DisplayName)).ToList());
    }

    public async Task<AdminMerchCampaignDetail?> GetAsync(Guid campaignId, string? locale, CancellationToken cancellationToken)
    {
        var storeId = ResolveStoreId();
        var campaign = await _campaigns.GetCampaignAsync(campaignId, storeId, cancellationToken);
        if (campaign is null)
        {
            return null;
        }

        var types = await _campaigns.ListPromotionTypeOptionsAsync(locale ?? "fa-IR", cancellationToken);
        var typeName = types.FirstOrDefault(t => t.Id == campaign.PromotionTypeId)?.DisplayName ?? "—";
        var translations = await _campaigns.ListCampaignTranslationsAsync(campaignId, cancellationToken);
        var members = await _campaigns.ResolveOrderedMembersAsync(campaignId, cancellationToken);
        var enriched = await EnrichMembersAsync(campaignId, members, campaign.StartAt, campaign.EndAt, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var runtime = DeriveRuntime(campaign.LifecycleStatus, campaign.StartAt, campaign.EndAt, now);
        return new AdminMerchCampaignDetail(
            campaign.Id,
            campaign.PromotionTypeId,
            typeName,
            campaign.LifecycleStatus.ToString(),
            runtime,
            campaign.StartAt,
            campaign.EndAt,
            campaign.Priority,
            campaign.UpdatedAt,
            translations.Select(t => new AdminMerchCampaignTranslationDto(t.Locale, t.Title, t.Subtitle, t.BadgeText)).ToList(),
            enriched);
    }

    public async Task<AdminMerchCampaignDetail> CreateAsync(
        AdminMerchCampaignWriteRequest body,
        CancellationToken cancellationToken)
    {
        ValidateWindow(body.StartAt, body.EndAt);
        ValidateTranslations(body.Translations);
        var storeId = ResolveStoreId();
        var created = await _campaigns.CreateCampaignAsync(
            body.PromotionTypeId,
            storeId,
            body.StartAt,
            body.EndAt,
            body.Priority,
            cancellationToken);
        foreach (var tr in body.Translations)
        {
            await _campaigns.UpsertCampaignTranslationAsync(
                created.Id,
                NormalizeLocale(tr.Locale),
                tr.Title.Trim(),
                tr.Subtitle,
                tr.BadgeText,
                cancellationToken);
        }

        return (await GetAsync(created.Id, "fa-IR", cancellationToken))!;
    }

    public async Task<AdminMerchCampaignDetail?> UpdateAsync(
        Guid campaignId,
        AdminMerchCampaignUpdateRequest body,
        CancellationToken cancellationToken)
    {
        ValidateWindow(body.StartAt, body.EndAt);
        ValidateTranslations(body.Translations);
        var storeId = ResolveStoreId();
        var existing = await _campaigns.GetCampaignAsync(campaignId, storeId, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        await _campaigns.UpdateCampaignWindowAsync(campaignId, body.StartAt, body.EndAt, body.Priority, cancellationToken);
        foreach (var tr in body.Translations)
        {
            await _campaigns.UpsertCampaignTranslationAsync(
                campaignId,
                NormalizeLocale(tr.Locale),
                tr.Title.Trim(),
                tr.Subtitle,
                tr.BadgeText,
                cancellationToken);
        }

        return await GetAsync(campaignId, "fa-IR", cancellationToken);
    }

    public async Task<bool> PublishAsync(Guid campaignId, CancellationToken cancellationToken)
    {
        var storeId = ResolveStoreId();
        var existing = await _campaigns.GetCampaignAsync(campaignId, storeId, cancellationToken);
        if (existing is null)
        {
            return false;
        }

        var translations = await _campaigns.ListCampaignTranslationsAsync(campaignId, cancellationToken);
        if (!translations.Any(t => !string.IsNullOrWhiteSpace(t.Title)))
        {
            throw new InvalidOperationException("برای انتشار حداقل یک عنوان ترجمه لازم است.");
        }

        if (existing.EndAt is { } end && end <= existing.StartAt)
        {
            throw new InvalidOperationException("بازهٔ زمانی کمپین نامعتبر است.");
        }

        await _campaigns.PublishCampaignAsync(campaignId, cancellationToken);
        return true;
    }

    public async Task<bool> ArchiveAsync(Guid campaignId, CancellationToken cancellationToken)
    {
        var storeId = ResolveStoreId();
        var existing = await _campaigns.GetCampaignAsync(campaignId, storeId, cancellationToken);
        if (existing is null)
        {
            return false;
        }

        await _campaigns.ArchiveCampaignAsync(campaignId, cancellationToken);
        return true;
    }

    public async Task<AdminMerchCampaignDetail?> AddMemberAsync(
        Guid campaignId,
        Guid sellerOfferId,
        CancellationToken cancellationToken)
    {
        var storeId = ResolveStoreId();
        var existing = await _campaigns.GetCampaignAsync(campaignId, storeId, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var offer = await _offers.Offers.AsNoTracking()
            .SingleOrDefaultAsync(x => x.OfferId == sellerOfferId, cancellationToken)
            ?? throw new InvalidOperationException("Offer یافت نشد.");
        if (offer.Status != OfferStatus.Active)
        {
            throw new InvalidOperationException("فقط Offer فعال قابل افزودن است.");
        }

        var members = await _campaigns.ResolveOrderedMembersAsync(campaignId, cancellationToken);
        if (members.Any(m => m.SellerOfferId == sellerOfferId))
        {
            throw new InvalidOperationException("این کالا قبلاً به کمپین اضافه شده است.");
        }

        var nextOrder = members.Count == 0 ? 0 : members.Max(m => m.SortOrder) + 1;
        await _campaigns.AddOfferAsync(campaignId, sellerOfferId, nextOrder, storeId, cancellationToken);
        return await GetAsync(campaignId, "fa-IR", cancellationToken);
    }

    public async Task<AdminMerchCampaignDetail?> RemoveMemberAsync(
        Guid campaignId,
        Guid sellerOfferId,
        CancellationToken cancellationToken)
    {
        var storeId = ResolveStoreId();
        if (await _campaigns.GetCampaignAsync(campaignId, storeId, cancellationToken) is null)
        {
            return null;
        }

        await _campaigns.RemoveOfferAsync(campaignId, sellerOfferId, cancellationToken);
        return await GetAsync(campaignId, "fa-IR", cancellationToken);
    }

    public async Task<AdminMerchCampaignDetail?> ReorderMembersAsync(
        Guid campaignId,
        IReadOnlyList<Guid> orderedSellerOfferIds,
        CancellationToken cancellationToken)
    {
        var storeId = ResolveStoreId();
        if (await _campaigns.GetCampaignAsync(campaignId, storeId, cancellationToken) is null)
        {
            return null;
        }

        await _campaigns.ReorderOffersAsync(campaignId, orderedSellerOfferIds, cancellationToken);
        return await GetAsync(campaignId, "fa-IR", cancellationToken);
    }

    public async Task<AdminMerchCampaignDetail?> SetMemberPriceAsync(
        Guid campaignId,
        Guid sellerOfferId,
        AdminMerchMemberPriceRequest body,
        CancellationToken cancellationToken)
    {
        if (body.Amount <= 0)
        {
            throw new InvalidOperationException("مبلغ کمپین باید بزرگ‌تر از صفر باشد.");
        }

        var storeId = ResolveStoreId();
        var campaign = await _campaigns.GetCampaignAsync(campaignId, storeId, cancellationToken);
        if (campaign is null)
        {
            return null;
        }

        var members = await _campaigns.ResolveOrderedMembersAsync(campaignId, cancellationToken);
        if (!members.Any(m => m.SellerOfferId == sellerOfferId))
        {
            throw new InvalidOperationException("این Offer عضو کمپین نیست.");
        }

        var market = string.IsNullOrWhiteSpace(body.Market) ? "IR" : body.Market.Trim();
        var currency = string.IsNullOrWhiteSpace(body.Currency) ? "IRR" : body.Currency.Trim().ToUpperInvariant();
        var channel = ParseChannel(body.Channel);
        var now = DateTimeOffset.UtcNow;
        var existing = await _priceLookup.ResolveCampaignPricesBatchAsync(
            [sellerOfferId],
            campaignId,
            market,
            channel,
            currency,
            now,
            cancellationToken);
        if (existing.TryGetValue(sellerOfferId, out var quote) && quote.PriceId != Guid.Empty)
        {
            await _prices.ChangeAmountAsync(quote.PriceId, body.Amount, currency, cancellationToken);
        }
        else
        {
            var created = await _prices.CreateCampaignPriceAsync(
                sellerOfferId,
                campaignId,
                market,
                channel,
                body.Amount,
                currency,
                campaign.StartAt < now ? campaign.StartAt : now.AddMinutes(-1),
                campaign.EndAt,
                cancellationToken);
            await _prices.ActivateAsync(created.PriceId, cancellationToken);
        }

        return await GetAsync(campaignId, "fa-IR", cancellationToken);
    }

    public async Task<AdminMerchOfferCandidateResponse> ListOfferCandidatesAsync(
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 50);
        var offers = await _offers.Offers.AsNoTracking()
            .Where(x => x.Status == OfferStatus.Active)
            .OrderByDescending(x => x.UpdatedAt)
            .Take(200)
            .ToListAsync(cancellationToken);
        var variantIds = offers.Select(o => o.CatalogVariantId).Distinct().ToArray();
        var variants = await _catalog.Variants.AsNoTracking()
            .Where(v => variantIds.Contains(v.VariantId))
            .Select(v => new { v.VariantId, v.ProductId })
            .ToListAsync(cancellationToken);
        var productIds = variants.Select(v => v.ProductId).Distinct().ToArray();
        var products = await _catalog.Products.AsNoTracking()
            .Where(p => productIds.Contains(p.ProductId))
            .Select(p => new { p.ProductId, p.SlugSeam })
            .ToListAsync(cancellationToken);
        var titles = await _catalog.LocalizedTexts.AsNoTracking()
            .Where(f => f.OwnerKind == CatalogLocalizedOwnerKind.Product
                        && productIds.Contains(f.OwnerId)
                        && f.FieldKey == "name"
                        && f.Locale == "fa-IR")
            .ToListAsync(cancellationToken);
        var sellerIds = offers.Select(o => o.SellerPartyId).Distinct().ToArray();
        var sellers = await _parties.Parties.AsNoTracking()
            .Where(o => sellerIds.Contains(o.PartyId))
            .Select(o => new { o.PartyId, o.DisplayName })
            .ToListAsync(cancellationToken);
        var variantToProduct = variants.ToDictionary(v => v.VariantId, v => v.ProductId);
        var productTitle = titles.ToDictionary(t => t.OwnerId, t => t.Value);
        var sellerName = sellers.ToDictionary(s => s.PartyId, s => s.DisplayName);
        var offerIds = offers.Select(o => o.OfferId).ToArray();
        var priceMap = await _priceLookup.ResolvePricesBatchAsync(
            offerIds,
            "IR",
            SalesChannel.Marketplace,
            "IRR",
            DateTimeOffset.UtcNow,
            cancellationToken);

        var rows = new List<AdminMerchOfferCandidate>();
        foreach (var offer in offers)
        {
            if (!variantToProduct.TryGetValue(offer.CatalogVariantId, out var productId))
            {
                continue;
            }

            var title = productTitle.GetValueOrDefault(productId)
                ?? products.FirstOrDefault(p => p.ProductId == productId)?.SlugSeam
                ?? "کالا";
            var seller = sellerName.GetValueOrDefault(offer.SellerPartyId) ?? "فروشنده";
            if (!string.IsNullOrWhiteSpace(search))
            {
                var needle = search.Trim();
                if (!title.Contains(needle, StringComparison.OrdinalIgnoreCase)
                    && !seller.Contains(needle, StringComparison.OrdinalIgnoreCase)
                    && !(offer.SellerSku?.Contains(needle, StringComparison.OrdinalIgnoreCase) ?? false))
                {
                    continue;
                }
            }

            priceMap.TryGetValue(offer.OfferId, out var price);
            var stock = await _availability.GetAvailabilityAsync(offer.OfferId, cancellationToken);
            var available = stock?.Available ?? 0;
            rows.Add(new AdminMerchOfferCandidate(
                offer.OfferId,
                title,
                seller,
                price?.Amount ?? 0,
                price?.Currency ?? "IRR",
                available,
                available > 0));
        }

        var total = rows.Count;
        var page = rows.Skip(skip).Take(take).ToList();
        return new AdminMerchOfferCandidateResponse(page, total);
    }

    private async Task<IReadOnlyList<AdminMerchCampaignMemberDto>> EnrichMembersAsync(
        Guid campaignId,
        IReadOnlyList<MerchandisingCampaignMemberReference> members,
        DateTimeOffset startAt,
        DateTimeOffset? endAt,
        CancellationToken cancellationToken)
    {
        if (members.Count == 0)
        {
            return Array.Empty<AdminMerchCampaignMemberDto>();
        }

        var offerIds = members.Select(m => m.SellerOfferId).ToArray();
        var offers = await _offers.Offers.AsNoTracking()
            .Where(o => offerIds.Contains(o.OfferId))
            .ToListAsync(cancellationToken);
        var offerMap = offers.ToDictionary(o => o.OfferId);
        var variantIds = offers.Select(o => o.CatalogVariantId).Distinct().ToArray();
        var variants = await _catalog.Variants.AsNoTracking()
            .Where(v => variantIds.Contains(v.VariantId))
            .ToListAsync(cancellationToken);
        var productIds = variants.Select(v => v.ProductId).Distinct().ToArray();
        var titles = await _catalog.LocalizedTexts.AsNoTracking()
            .Where(f => f.OwnerKind == CatalogLocalizedOwnerKind.Product
                        && productIds.Contains(f.OwnerId)
                        && f.FieldKey == "name"
                        && f.Locale == "fa-IR")
            .ToListAsync(cancellationToken);
        var sellerIds = offers.Select(o => o.SellerPartyId).Distinct().ToArray();
        var sellers = await _parties.Parties.AsNoTracking()
            .Where(o => sellerIds.Contains(o.PartyId))
            .Select(o => new { o.PartyId, o.DisplayName })
            .ToListAsync(cancellationToken);
        var variantToProduct = variants.ToDictionary(v => v.VariantId, v => v.ProductId);
        var productTitle = titles.ToDictionary(t => t.OwnerId, t => t.Value);
        var sellerName = sellers.ToDictionary(s => s.PartyId, s => s.DisplayName);
        var now = DateTimeOffset.UtcNow;
        var basePrices = await _priceLookup.ResolvePricesBatchAsync(
            offerIds, "IR", SalesChannel.Marketplace, "IRR", now, cancellationToken);
        var campaignPrices = await _priceLookup.ResolveCampaignPricesBatchAsync(
            offerIds, campaignId, "IR", SalesChannel.Marketplace, "IRR", now, cancellationToken);

        var result = new List<AdminMerchCampaignMemberDto>(members.Count);
        foreach (var member in members.OrderBy(m => m.SortOrder).ThenBy(m => m.MembershipId))
        {
            if (!offerMap.TryGetValue(member.SellerOfferId, out var offer))
            {
                continue;
            }

            variantToProduct.TryGetValue(offer.CatalogVariantId, out var productId);
            var title = productId != Guid.Empty && productTitle.TryGetValue(productId, out var t) ? t : "کالا";
            var seller = sellerName.GetValueOrDefault(offer.SellerPartyId) ?? "فروشنده";
            basePrices.TryGetValue(member.SellerOfferId, out var baseQuote);
            campaignPrices.TryGetValue(member.SellerOfferId, out var campQuote);
            var stock = await _availability.GetAvailabilityAsync(member.SellerOfferId, cancellationToken);
            var available = stock?.Available ?? 0;
            result.Add(new AdminMerchCampaignMemberDto(
                member.SellerOfferId,
                member.SortOrder,
                title,
                seller,
                baseQuote?.Amount ?? 0,
                campQuote?.Amount,
                campQuote?.Currency ?? baseQuote?.Currency ?? "IRR",
                available,
                available > 0));
        }

        return result;
    }

    private static void ValidateWindow(DateTimeOffset startAt, DateTimeOffset? endAt)
    {
        if (endAt is { } end && end <= startAt)
        {
            throw new InvalidOperationException("زمان پایان باید بعد از زمان شروع باشد.");
        }
    }

    private static void ValidateTranslations(IReadOnlyList<AdminMerchCampaignTranslationDto>? translations)
    {
        if (translations is null || translations.Count == 0 || translations.All(t => string.IsNullOrWhiteSpace(t.Title)))
        {
            throw new InvalidOperationException("عنوان کمپین الزامی است.");
        }
    }

    private static string NormalizeLocale(string locale)
    {
        var raw = string.IsNullOrWhiteSpace(locale) ? "fa-IR" : locale.Trim();
        if (raw.Equals("fa", StringComparison.OrdinalIgnoreCase)) return "fa-IR";
        if (raw.Equals("en", StringComparison.OrdinalIgnoreCase)) return "en-US";
        return raw;
    }

    private static SalesChannel ParseChannel(string? channel)
    {
        if (string.IsNullOrWhiteSpace(channel))
        {
            return SalesChannel.Marketplace;
        }

        return Enum.TryParse<SalesChannel>(channel, true, out var parsed) ? parsed : SalesChannel.Marketplace;
    }

    private static string DeriveRuntime(
        MerchandisingCampaignLifecycleStatus lifecycle,
        DateTimeOffset startAt,
        DateTimeOffset? endAt,
        DateTimeOffset now)
    {
        if (lifecycle == MerchandisingCampaignLifecycleStatus.Draft) return "draft";
        if (lifecycle == MerchandisingCampaignLifecycleStatus.Archived) return "archived";
        if (startAt > now) return "scheduled";
        if (endAt is { } end && end <= now) return "expired";
        return "active";
    }
}

/// <summary>مسیرهای Admin کمپین‌های فروش (مرچندایزینگ).</summary>
public static class MerchandisingCampaignAdminEndpoints
{
    public static void MapMerchandisingCampaignAdminEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/v1/admin/merchandising-campaigns");
        group.MapGet("/", ListAsync);
        group.MapGet("/types", ListTypesAsync);
        group.MapGet("/offer-candidates", ListCandidatesAsync);
        group.MapGet("/{campaignId:guid}", GetAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{campaignId:guid}", UpdateAsync);
        group.MapPost("/{campaignId:guid}/publish", PublishAsync);
        group.MapPost("/{campaignId:guid}/archive", ArchiveAsync);
        group.MapPost("/{campaignId:guid}/members", AddMemberAsync);
        group.MapDelete("/{campaignId:guid}/members/{sellerOfferId:guid}", RemoveMemberAsync);
        group.MapPut("/{campaignId:guid}/members/order", ReorderAsync);
        group.MapPut("/{campaignId:guid}/members/{sellerOfferId:guid}/price", SetPriceAsync);
    }

    private static async Task<IResult> ListAsync(
        MerchandisingCampaignAdminComposer composer,
        string? search,
        string? lifecycle,
        Guid? promotionTypeId,
        string? runtimeWindow,
        int? skip,
        int? take,
        string? locale,
        CancellationToken cancellationToken)
    {
        var result = await composer.ListAsync(
            search,
            lifecycle,
            promotionTypeId,
            runtimeWindow,
            skip ?? 0,
            take ?? 25,
            locale,
            cancellationToken);
        return Results.Json(result);
    }

    private static async Task<IResult> ListTypesAsync(
        MerchandisingCampaignAdminComposer composer,
        string? locale,
        CancellationToken cancellationToken)
        => Results.Json(await composer.ListTypesAsync(locale, cancellationToken));

    private static async Task<IResult> ListCandidatesAsync(
        MerchandisingCampaignAdminComposer composer,
        string? search,
        int? skip,
        int? take,
        CancellationToken cancellationToken)
        => Results.Json(await composer.ListOfferCandidatesAsync(search, skip ?? 0, take ?? 20, cancellationToken));

    private static async Task<IResult> GetAsync(
        Guid campaignId,
        MerchandisingCampaignAdminComposer composer,
        string? locale,
        CancellationToken cancellationToken)
    {
        var detail = await composer.GetAsync(campaignId, locale, cancellationToken);
        return detail is null ? Results.NotFound() : Results.Json(detail);
    }

    private static async Task<IResult> CreateAsync(
        AdminMerchCampaignWriteRequest body,
        MerchandisingCampaignAdminComposer composer,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Json(await composer.CreateAsync(body, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { title = ex.Message, errorCode = "campaign.validation" });
        }
    }

    private static async Task<IResult> UpdateAsync(
        Guid campaignId,
        AdminMerchCampaignUpdateRequest body,
        MerchandisingCampaignAdminComposer composer,
        CancellationToken cancellationToken)
    {
        try
        {
            var detail = await composer.UpdateAsync(campaignId, body, cancellationToken);
            return detail is null ? Results.NotFound() : Results.Json(detail);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { title = ex.Message, errorCode = "campaign.validation" });
        }
    }

    private static async Task<IResult> PublishAsync(
        Guid campaignId,
        MerchandisingCampaignAdminComposer composer,
        CancellationToken cancellationToken)
    {
        try
        {
            return await composer.PublishAsync(campaignId, cancellationToken)
                ? Results.Ok()
                : Results.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { title = ex.Message, errorCode = "campaign.publish" });
        }
    }

    private static async Task<IResult> ArchiveAsync(
        Guid campaignId,
        MerchandisingCampaignAdminComposer composer,
        CancellationToken cancellationToken)
        => await composer.ArchiveAsync(campaignId, cancellationToken) ? Results.Ok() : Results.NotFound();

    private static async Task<IResult> AddMemberAsync(
        Guid campaignId,
        AdminMerchAddMemberRequest body,
        MerchandisingCampaignAdminComposer composer,
        CancellationToken cancellationToken)
    {
        try
        {
            var detail = await composer.AddMemberAsync(campaignId, body.SellerOfferId, cancellationToken);
            return detail is null ? Results.NotFound() : Results.Json(detail);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { title = ex.Message, errorCode = "campaign.member" });
        }
    }

    private static async Task<IResult> RemoveMemberAsync(
        Guid campaignId,
        Guid sellerOfferId,
        MerchandisingCampaignAdminComposer composer,
        CancellationToken cancellationToken)
    {
        var detail = await composer.RemoveMemberAsync(campaignId, sellerOfferId, cancellationToken);
        return detail is null ? Results.NotFound() : Results.Json(detail);
    }

    private static async Task<IResult> ReorderAsync(
        Guid campaignId,
        AdminMerchReorderMembersRequest body,
        MerchandisingCampaignAdminComposer composer,
        CancellationToken cancellationToken)
    {
        try
        {
            var detail = await composer.ReorderMembersAsync(campaignId, body.OrderedSellerOfferIds, cancellationToken);
            return detail is null ? Results.NotFound() : Results.Json(detail);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { title = ex.Message, errorCode = "campaign.reorder" });
        }
    }

    private static async Task<IResult> SetPriceAsync(
        Guid campaignId,
        Guid sellerOfferId,
        AdminMerchMemberPriceRequest body,
        MerchandisingCampaignAdminComposer composer,
        CancellationToken cancellationToken)
    {
        try
        {
            var detail = await composer.SetMemberPriceAsync(campaignId, sellerOfferId, body, cancellationToken);
            return detail is null ? Results.NotFound() : Results.Json(detail);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { title = ex.Message, errorCode = "campaign.price" });
        }
    }
}
