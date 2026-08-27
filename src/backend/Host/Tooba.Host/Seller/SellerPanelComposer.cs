using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Inventory.Application;
using Tooba.Inventory.Domain;
using Tooba.Inventory.Infrastructure.Persistence;
using Tooba.Offer.Application;
using Tooba.Offer.Domain;
using Tooba.Offer.Infrastructure.Persistence;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Party.Application;
using Tooba.Pricing.Application;
using Tooba.Pricing.Domain;
using Tooba.Pricing.Infrastructure.Persistence;
using Tooba.Tax.Application;
using Tooba.Tax.Domain;
using Tooba.Tax.Infrastructure.Persistence;

namespace Tooba.Host.Seller;

/// <summary>
/// ترکیب HTTP پنل فروشنده. هر DbContext جدا پرس‌وجو می‌شود؛ فیلتر Seller در سرور است نه در UI.
/// </summary>
public sealed class SellerPanelComposer
{
    /// <summary>بازار پیش‌فرض store-alpha / دمو ایران.</summary>
    public const string DefaultMarket = "IR";

    /// <summary>ارز نوشته‌شدهٔ پیش‌فرض؛ تومان نمایشی نیست.</summary>
    public const string DefaultCurrency = "IRR";

    private readonly OfferDbContext _offers;
    private readonly CatalogDbContext _catalog;
    private readonly PricingDbContext _prices;
    private readonly InventoryDbContext _inventory;
    private readonly OrderDbContext _orders;
    private readonly IPartyLookupGateway _parties;
    private readonly IOfferDirectory _offerDirectory;
    private readonly IPriceDirectory _priceDirectory;
    private readonly IInventoryDirectory _inventoryDirectory;
    private readonly ITaxDirectory _taxDirectory;
    private readonly TaxDbContext _tax;

    /// <summary>
    /// سازندهٔ ترکیب فروشنده بدون JOIN بین‌schema؛ نوشتن تجاری از دایرکتوری‌های مالک.
    /// </summary>
    public SellerPanelComposer(
        OfferDbContext offers,
        CatalogDbContext catalog,
        PricingDbContext prices,
        InventoryDbContext inventory,
        OrderDbContext orders,
        IPartyLookupGateway parties,
        IOfferDirectory offerDirectory,
        IPriceDirectory priceDirectory,
        IInventoryDirectory inventoryDirectory,
        ITaxDirectory taxDirectory,
        TaxDbContext tax)
    {
        _offers = offers;
        _catalog = catalog;
        _prices = prices;
        _inventory = inventory;
        _orders = orders;
        _parties = parties;
        _offerDirectory = offerDirectory;
        _priceDirectory = priceDirectory;
        _inventoryDirectory = inventoryDirectory;
        _taxDirectory = taxDirectory;
        _tax = tax;
    }

    /// <summary>
    /// خلاصهٔ داشبورد واقعی برای همان SellerPartyId.
    /// </summary>
    public async Task<SellerDashboardSummary> GetDashboardAsync(Guid sellerPartyId, CancellationToken cancellationToken)
    {
        await EnsureSellerAsync(sellerPartyId, cancellationToken);
        var seller = await _parties.FindByIdAsync(sellerPartyId, cancellationToken)
            ?? throw new PlatformHttpException(404, "فروشنده پیدا نشد.", "seller.missing");
        var activeOffers = await _offers.Offers.AsNoTracking()
            .CountAsync(x => x.SellerPartyId == sellerPartyId && x.Status == OfferStatus.Active, cancellationToken);
        var sellerOrders = await _orders.SellerOrders.AsNoTracking()
            .Where(x => x.SellerPartyId == sellerPartyId)
            .Select(x => x.Status)
            .ToListAsync(cancellationToken);
        var open = sellerOrders.Count(x =>
            x is SellerOrderStatus.Submitted
                or SellerOrderStatus.PendingPayment
                or SellerOrderStatus.ReservationRequested);
        var paid = sellerOrders.Count(x => x == SellerOrderStatus.Paid);
        return new SellerDashboardSummary(sellerPartyId, seller.DisplayName, activeOffers, open, paid);
    }

    /// <summary>
    /// فهرست Offerهای همان فروشنده با قیمت/موجودی جداگانه.
    /// </summary>
    public async Task<IReadOnlyList<SellerOfferListItem>> ListOffersAsync(Guid sellerPartyId, CancellationToken cancellationToken)
    {
        await EnsureSellerAsync(sellerPartyId, cancellationToken);
        var offers = await _offers.Offers.AsNoTracking()
            .Where(x => x.SellerPartyId == sellerPartyId && x.Status != OfferStatus.Archived)
            .OrderByDescending(x => x.OfferId)
            .Take(200)
            .ToListAsync(cancellationToken);
        return await PresentOffersAsync(offers, cancellationToken);
    }

    /// <summary>
    /// شناسهٔ محصولات Catalog متعلق به Offerهای غیرآرشیو همین فروشنده را بدون JOIN بین‌schema برمی‌گرداند.
    /// </summary>
    public async Task<IReadOnlyList<Guid>> ListOwnedProductIdsAsync(Guid sellerPartyId, CancellationToken cancellationToken)
    {
        await EnsureSellerAsync(sellerPartyId, cancellationToken);
        var variantIds = await _offers.Offers.AsNoTracking()
            .Where(x => x.SellerPartyId == sellerPartyId && x.Status != OfferStatus.Archived)
            .Select(x => x.CatalogVariantId)
            .Distinct()
            .ToListAsync(cancellationToken);
        if (variantIds.Count == 0)
        {
            return [];
        }

        return await _catalog.Variants.AsNoTracking()
            .Where(x => variantIds.Contains(x.VariantId))
            .Select(x => x.ProductId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// جزئیات Offer فقط اگر متعلق به فروشندهٔ جاری باشد.
    /// </summary>
    public async Task<SellerOfferDetailPage?> GetOfferAsync(Guid sellerPartyId, Guid offerId, CancellationToken cancellationToken)
    {
        await EnsureSellerAsync(sellerPartyId, cancellationToken);
        var offer = await _offers.Offers.AsNoTracking()
            .SingleOrDefaultAsync(x => x.OfferId == offerId && x.SellerPartyId == sellerPartyId, cancellationToken);
        if (offer is null)
        {
            return null;
        }

        var items = await PresentOffersAsync([offer], cancellationToken);
        var row = items[0];
        var seller = await _parties.FindByIdAsync(sellerPartyId, cancellationToken);
        var price = await _prices.Prices.AsNoTracking()
            .Where(x => x.OfferId == offerId)
            .OrderByDescending(x => x.PriceId)
            .Select(x => new { x.Amount, x.Currency })
            .FirstOrDefaultAsync(cancellationToken);
        var stock = await _inventory.Positions.AsNoTracking()
            .Where(x => x.OfferId == offerId)
            .Select(x => new { x.OnHand, x.Reserved })
            .FirstOrDefaultAsync(cancellationToken);
        string? brand = null;
        if (row.ProductId is Guid productId)
        {
            var brandId = await _catalog.Products.AsNoTracking()
                .Where(x => x.ProductId == productId)
                .Select(x => x.BrandId)
                .FirstOrDefaultAsync(cancellationToken);
            if (brandId is Guid bid)
            {
                brand = await _catalog.LocalizedTexts.AsNoTracking()
                    .Where(x => x.OwnerKind == CatalogLocalizedOwnerKind.Brand && x.OwnerId == bid && x.FieldKey == "name")
                    .OrderBy(x => x.Locale)
                    .Select(x => x.Value)
                    .FirstOrDefaultAsync(cancellationToken);
            }
        }

        var onHand = stock?.OnHand ?? 0;
        var reserved = stock?.Reserved ?? 0;
        return new SellerOfferDetailPage(
            offer.OfferId,
            sellerPartyId,
            seller?.DisplayName ?? string.Empty,
            offer.CatalogVariantId,
            row.ProductId,
            row.ProductTitle,
            brand,
            offer.SellerSku,
            offer.Status.ToString(),
            offer.Channel.ToString(),
            price?.Amount,
            price?.Currency ?? row.Currency,
            onHand,
            reserved,
            Math.Max(0, onHand - reserved),
            CatalogReadOnly: true);
    }

    /// <summary>
    /// به‌روزرسانی باریک SKU/وضعیت Offer متعلق به همان فروشنده.
    /// </summary>
    public async Task<SellerOfferDetailPage> PatchOfferAsync(
        Guid sellerPartyId,
        Guid offerId,
        SellerOfferPatchRequest patch,
        CancellationToken cancellationToken)
    {
        await EnsureSellerAsync(sellerPartyId, cancellationToken);
        var offer = await _offers.Offers
            .SingleOrDefaultAsync(x => x.OfferId == offerId && x.SellerPartyId == sellerPartyId, cancellationToken)
            ?? throw new PlatformHttpException(404, "پیشنهاد فروشنده پیدا نشد.", "seller.offer.missing");

        if (patch.SellerSku is not null)
        {
            var sku = patch.SellerSku.Trim();
            if (sku.Length > 0)
            {
                var dup = await _offers.Offers.AnyAsync(
                    x => x.SellerPartyId == sellerPartyId && x.SellerSku == sku && x.OfferId != offerId,
                    cancellationToken);
                if (dup)
                {
                    throw new PlatformHttpException(409, "کد فروشنده تکراری است.", "seller.offer.sku.conflict");
                }
            }

            offer.SellerSku = string.IsNullOrWhiteSpace(sku) ? null : sku;
            offer.UpdatedAt = DateTimeOffset.UtcNow;
        }

        if (!string.IsNullOrWhiteSpace(patch.Status))
        {
            if (string.Equals(patch.Status, nameof(OfferStatus.Active), StringComparison.OrdinalIgnoreCase))
            {
                offer.Activate(DateTimeOffset.UtcNow);
            }
            else if (string.Equals(patch.Status, nameof(OfferStatus.Suspended), StringComparison.OrdinalIgnoreCase))
            {
                offer.Suspend(DateTimeOffset.UtcNow);
            }
            else
            {
                throw new PlatformHttpException(400, "وضعیت پشتیبانی‌شده نیست.", "seller.offer.status.unsupported");
            }
        }

        await _offers.SaveChangesAsync(cancellationToken);
        return (await GetOfferAsync(sellerPartyId, offerId, cancellationToken))!;
    }

    /// <summary>
    /// گونه‌های Catalog منتشرشده را برای انتخاب Offer برمی‌گرداند؛ نوشتن Catalog نیست.
    /// </summary>
    public async Task<IReadOnlyList<SellerCatalogVariantOption>> ListCatalogVariantsAsync(
        Guid sellerPartyId,
        CancellationToken cancellationToken)
    {
        await EnsureSellerAsync(sellerPartyId, cancellationToken);
        var products = await _catalog.Products.AsNoTracking()
            .Where(x => x.Status == CatalogPublicationStatus.Published)
            .OrderByDescending(x => x.UpdatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);
        if (products.Count == 0)
        {
            return [];
        }

        var productIds = products.Select(x => x.ProductId).ToList();
        var names = await _catalog.LocalizedTexts.AsNoTracking()
            .Where(x => x.OwnerKind == CatalogLocalizedOwnerKind.Product
                        && productIds.Contains(x.OwnerId)
                        && x.FieldKey == "name")
            .ToListAsync(cancellationToken);
        var nameMap = names
            .GroupBy(x => x.OwnerId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(x => x.Locale.StartsWith("fa", StringComparison.OrdinalIgnoreCase) ? 0 : 1).First().Value);
        var variants = await _catalog.Variants.AsNoTracking()
            .Where(x => productIds.Contains(x.ProductId))
            .OrderBy(x => x.CatalogCodeSeam)
            .ToListAsync(cancellationToken);
        var productStatus = products.ToDictionary(x => x.ProductId, x => x.Status.ToString());
        return variants.Select(variant => new SellerCatalogVariantOption(
            variant.VariantId,
            variant.ProductId,
            nameMap.GetValueOrDefault(variant.ProductId) ?? "بدون عنوان",
            variant.CatalogCodeSeam,
            productStatus.GetValueOrDefault(variant.ProductId) ?? "Published")).ToList();
    }

    /// <summary>
    /// Offer را فقط برای Party فروشندهٔ احرازشده می‌سازد؛ بدنه شناسهٔ فروشندهٔ خارجی را نمی‌پذیرد.
    /// </summary>
    public async Task<SellerOfferDetailPage> CreateOfferAsync(
        Guid sellerPartyId,
        SellerOfferCreateRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureSellerAsync(sellerPartyId, cancellationToken);
        if (request.CatalogVariantId == Guid.Empty)
        {
            throw new PlatformHttpException(400, "گونهٔ Catalog لازم است.", "seller.offer.variant.missing");
        }

        OfferReference created;
        try
        {
            created = await _offerDirectory.CreateOfferAsync(
                request.CatalogVariantId,
                sellerPartyId,
                SalesChannel.Marketplace,
                request.SellerSku,
                cancellationToken);
            if (string.Equals(request.Status, nameof(OfferStatus.Active), StringComparison.OrdinalIgnoreCase))
            {
                await _offerDirectory.ActivateAsync(created.OfferId, cancellationToken);
            }
            else if (!string.IsNullOrWhiteSpace(request.Status)
                     && !string.Equals(request.Status, nameof(OfferStatus.Draft), StringComparison.OrdinalIgnoreCase))
            {
                throw new PlatformHttpException(400, "وضعیت پشتیبانی‌شده نیست.", "seller.offer.status.unsupported");
            }

            await EnsureOfferTaxCoverageAsync(created.OfferId, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new PlatformHttpException(400, ex.Message, "seller.offer.create.rejected");
        }

        return (await GetOfferAsync(sellerPartyId, created.OfferId, cancellationToken))!;
    }

    /// <summary>
    /// طبقه/قاعدهٔ مالیاتی استاندارد را برای Offer تازه تضمین می‌کند تا Checkout با TAX_NO_APPLICABLE_RULE نشکند.
    /// </summary>
    private async Task EnsureOfferTaxCoverageAsync(Guid offerId, CancellationToken cancellationToken)
    {
        var category = await _tax.Categories.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Code == "standard" || x.Code == "standard-demo",
                cancellationToken);
        TaxCategoryReference categoryRef;
        if (category is null)
        {
            categoryRef = await _taxDirectory.CreateCategoryAsync("standard", "استاندارد", cancellationToken);
        }
        else
        {
            categoryRef = new TaxCategoryReference(category.CategoryId, category.Code, category.DisplayName);
        }

        var hasActiveRule = await _tax.Rules.AsNoTracking()
            .AnyAsync(
                rule => rule.CategoryId == categoryRef.CategoryId
                    && rule.Jurisdiction == "IR-NAT"
                    && rule.Market == DefaultMarket
                    && rule.Status == TaxRuleStatus.Active,
                cancellationToken);
        if (!hasActiveRule)
        {
            var rule = await _taxDirectory.CreateRuleAsync(
                "IR-NAT",
                DefaultMarket,
                categoryRef.CategoryId,
                TaxRuleKind.Percentage,
                0.09m,
                DateTimeOffset.UtcNow.AddYears(-1),
                null,
                100,
                TaxOverridePolicy.Disabled,
                cancellationToken);
            await _taxDirectory.ActivateRuleAsync(rule.RuleId, cancellationToken);
        }

        await _taxDirectory.AssignOfferCategoryAsync(offerId, categoryRef.CategoryId, cancellationToken);
    }

    /// <summary>
    /// مبلغ بدون مالیات Offer خود فروشنده را از طریق IPriceDirectory می‌نویسد؛ Offer خارجی رد می‌شود.
    /// </summary>
    public async Task<SellerOfferDetailPage> SetOfferPriceAsync(
        Guid sellerPartyId,
        Guid offerId,
        SellerOfferPriceWriteRequest request,
        CancellationToken cancellationToken)
    {
        var offer = await RequireOwnedOfferAsync(sellerPartyId, offerId, cancellationToken);
        if (request.Amount < 0)
        {
            throw new PlatformHttpException(400, "مبلغ منفی مجاز نیست.", "seller.price.amount.invalid");
        }

        var market = string.IsNullOrWhiteSpace(request.Market) ? DefaultMarket : request.Market.Trim();
        var currency = string.IsNullOrWhiteSpace(request.Currency) ? DefaultCurrency : request.Currency.Trim();

        try
        {
            var existing = await _prices.Prices
                .AsNoTracking()
                .Where(x => x.OfferId == offerId
                            && x.Market == market
                            && x.Channel == offer.Channel
                            && x.Currency == currency)
                .OrderByDescending(x => x.PriceId)
                .FirstOrDefaultAsync(cancellationToken);

            if (existing is null || existing.Status == PriceStatus.Retired)
            {
                var created = await _priceDirectory.CreatePriceAsync(
                    offerId,
                    market,
                    offer.Channel,
                    request.Amount,
                    currency,
                    DateTimeOffset.UtcNow.AddYears(-1),
                    null,
                    cancellationToken);
                await _priceDirectory.ActivateAsync(created.PriceId, cancellationToken);
            }
            else
            {
                await _priceDirectory.ChangeAmountAsync(existing.PriceId, request.Amount, currency, cancellationToken);
                if (existing.Status != PriceStatus.Active)
                {
                    await _priceDirectory.ActivateAsync(existing.PriceId, cancellationToken);
                }
            }
        }
        catch (InvalidOperationException ex)
        {
            throw new PlatformHttpException(400, ex.Message, "seller.price.write.rejected");
        }

        return (await GetOfferAsync(sellerPartyId, offerId, cancellationToken))!;
    }

    /// <summary>
    /// موجودی روی‌دست Offer خود فروشنده را از طریق IInventoryDirectory تنظیم می‌کند؛ Offer خارجی رد می‌شود.
    /// </summary>
    public async Task<SellerOfferDetailPage> SetOfferInventoryAsync(
        Guid sellerPartyId,
        Guid offerId,
        SellerOfferInventoryWriteRequest request,
        CancellationToken cancellationToken)
    {
        await RequireOwnedOfferAsync(sellerPartyId, offerId, cancellationToken);
        if (request.OnHand < 0)
        {
            throw new PlatformHttpException(400, "موجودی منفی مجاز نیست.", "seller.inventory.quantity.invalid");
        }

        var reason = string.IsNullOrWhiteSpace(request.Reason) ? "seller-panel-adjust" : request.Reason.Trim();
        try
        {
            var position = await _inventory.Positions.AsNoTracking()
                .Where(x => x.OfferId == offerId)
                .OrderBy(x => x.StockItemId)
                .FirstOrDefaultAsync(cancellationToken);
            Guid stockItemId;
            if (position is null)
            {
                var locationId = await ResolveDefaultLocationIdAsync(cancellationToken);
                stockItemId = await _inventoryDirectory.OpenPositionAsync(offerId, locationId, cancellationToken);
            }
            else
            {
                stockItemId = position.StockItemId;
            }

            await _inventoryDirectory.AdjustAsync(
                stockItemId,
                StockAdjustmentKind.Set,
                request.OnHand,
                reason,
                null,
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new PlatformHttpException(400, ex.Message, "seller.inventory.write.rejected");
        }

        return (await GetOfferAsync(sellerPartyId, offerId, cancellationToken))!;
    }

    /// <summary>
    /// فهرست سفارش‌های فقط همین فروشنده.
    /// </summary>
    public async Task<IReadOnlyList<SellerOrderListItem>> ListOrdersAsync(Guid sellerPartyId, CancellationToken cancellationToken)
    {
        await EnsureSellerAsync(sellerPartyId, cancellationToken);
        var orders = await _orders.SellerOrders.AsNoTracking()
            .Include(x => x.Lines)
            .Where(x => x.SellerPartyId == sellerPartyId)
            .OrderByDescending(x => x.SellerOrderId)
            .Take(200)
            .ToListAsync(cancellationToken);
        var checkoutIds = orders.Select(x => x.CheckoutId).Distinct().ToList();
        var checkouts = checkoutIds.Count == 0
            ? []
            : await _orders.Checkouts.AsNoTracking()
                .Where(x => checkoutIds.Contains(x.CheckoutId))
                .ToListAsync(cancellationToken);
        var checkoutMap = checkouts.ToDictionary(x => x.CheckoutId);
        return orders.Select(order =>
        {
            checkoutMap.TryGetValue(order.CheckoutId, out var checkout);
            return new SellerOrderListItem(
                order.SellerOrderId,
                order.OrderNumber,
                checkout?.SubmittedAt ?? default,
                checkout?.RecipientName ?? string.Empty,
                order.Lines.Count,
                order.GrandTotalSnapshot,
                order.Currency,
                order.Status.ToString(),
                order.Status.ToString());
        }).ToList();
    }

    /// <summary>
    /// جزئیات سفارش فقط اگر SellerPartyId مطابقت کند؛ خطوط دیگران برنمی‌گردد.
    /// </summary>
    public async Task<SellerOrderDetailPage?> GetOrderAsync(Guid sellerPartyId, Guid sellerOrderId, CancellationToken cancellationToken)
    {
        await EnsureSellerAsync(sellerPartyId, cancellationToken);
        var order = await _orders.SellerOrders.AsNoTracking()
            .Include(x => x.Lines)
            .SingleOrDefaultAsync(x => x.SellerOrderId == sellerOrderId && x.SellerPartyId == sellerPartyId, cancellationToken);
        if (order is null)
        {
            return null;
        }

        var checkout = await _orders.Checkouts.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CheckoutId == order.CheckoutId, cancellationToken);
        var offerIds = order.Lines.Select(x => x.OfferId).Distinct().ToList();
        var offers = offerIds.Count == 0
            ? []
            : await _offers.Offers.AsNoTracking().Where(x => offerIds.Contains(x.OfferId)).ToListAsync(cancellationToken);
        var titles = await ResolveTitlesAsync(offers, cancellationToken);
        var lines = order.Lines.Select(line =>
        {
            titles.TryGetValue(line.OfferId, out var title);
            return new SellerOrderLineView(
                line.OfferId,
                string.IsNullOrWhiteSpace(title) ? "کالای سفارش" : title,
                line.Quantity,
                line.UnitPriceSnapshot,
                line.LineTotalSnapshot + line.TaxAmountSnapshot - line.DiscountAmountSnapshot,
                line.Currency);
        }).ToList();

        return new SellerOrderDetailPage(
            order.SellerOrderId,
            order.OrderNumber,
            order.SellerPartyId,
            checkout?.SubmittedAt ?? default,
            order.Status.ToString(),
            order.Status.ToString(),
            order.SubtotalSnapshot,
            order.TaxSnapshot,
            order.DiscountSnapshot,
            order.GrandTotalSnapshot,
            order.Currency,
            checkout?.RecipientName ?? string.Empty,
            checkout?.ContactMobile ?? string.Empty,
            checkout?.ProvinceName ?? string.Empty,
            checkout?.CityName ?? string.Empty,
            checkout?.PostalAddress ?? string.Empty,
            checkout?.PostalCode ?? string.Empty,
            checkout?.ShippingMethodLabel ?? string.Empty,
            lines);
    }

    private async Task EnsureSellerAsync(Guid sellerPartyId, CancellationToken cancellationToken)
    {
        var seller = await _parties.FindByIdAsync(sellerPartyId, cancellationToken);
        if (seller is null)
        {
            throw new PlatformHttpException(404, "فروشنده پیدا نشد.", "seller.missing");
        }
    }

    /// <summary>
    /// Offer را فقط اگر متعلق به فروشندهٔ جاری باشد برمی‌گرداند؛ در غیر این صورت fail-closed است.
    /// </summary>
    private async Task<SellerOffer> RequireOwnedOfferAsync(
        Guid sellerPartyId,
        Guid offerId,
        CancellationToken cancellationToken)
    {
        await EnsureSellerAsync(sellerPartyId, cancellationToken);
        var offer = await _offers.Offers.AsNoTracking()
            .SingleOrDefaultAsync(x => x.OfferId == offerId && x.SellerPartyId == sellerPartyId, cancellationToken);
        if (offer is null)
        {
            throw new PlatformHttpException(404, "پیشنهاد فروشنده پیدا نشد.", "seller.offer.missing");
        }

        return offer;
    }

    /// <summary>
    /// محل نگهداری پیش‌فرض را پیدا یا می‌سازد تا موقعیت موجودی Offer باز شود.
    /// </summary>
    private async Task<Guid> ResolveDefaultLocationIdAsync(CancellationToken cancellationToken)
    {
        var existing = await _inventory.Locations.AsNoTracking()
            .Where(x => x.Status == InventoryLocationStatus.Active)
            .OrderBy(x => x.Code)
            .Select(x => x.LocationId)
            .FirstOrDefaultAsync(cancellationToken);
        if (existing != Guid.Empty)
        {
            return existing;
        }

        return await _inventoryDirectory.CreateLocationAsync("SELLER-DEFAULT", "انبار پیش‌فرض فروشنده", cancellationToken);
    }

    private async Task<IReadOnlyList<SellerOfferListItem>> PresentOffersAsync(
        IReadOnlyList<SellerOffer> offers,
        CancellationToken cancellationToken)
    {
        if (offers.Count == 0)
        {
            return [];
        }

        var offerIds = offers.Select(x => x.OfferId).ToList();
        var variantIds = offers.Select(x => x.CatalogVariantId).Distinct().ToList();
        var variants = await _catalog.Variants.AsNoTracking()
            .Where(x => variantIds.Contains(x.VariantId))
            .Select(x => new { x.VariantId, x.ProductId })
            .ToListAsync(cancellationToken);
        var productIds = variants.Select(x => x.ProductId).Distinct().ToList();
        var names = new Dictionary<Guid, string>();
        if (productIds.Count > 0)
        {
            var nameRows = await _catalog.LocalizedTexts.AsNoTracking()
                .Where(x => x.OwnerKind == CatalogLocalizedOwnerKind.Product && productIds.Contains(x.OwnerId) && x.FieldKey == "name")
                .ToListAsync(cancellationToken);
            names = nameRows
                .GroupBy(x => x.OwnerId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(x => x.Locale.StartsWith("fa", StringComparison.OrdinalIgnoreCase) ? 0 : 1).First().Value);
        }
        var amounts = await _prices.Prices.AsNoTracking()
            .Where(x => offerIds.Contains(x.OfferId))
            .Select(x => new { x.OfferId, x.Amount, x.Currency, x.PriceId })
            .ToListAsync(cancellationToken);
        var amountMap = amounts
            .GroupBy(x => x.OfferId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.PriceId).First());
        var stocks = await _inventory.Positions.AsNoTracking()
            .Where(x => offerIds.Contains(x.OfferId))
            .Select(x => new { x.OfferId, x.OnHand, x.Reserved })
            .ToListAsync(cancellationToken);
        var stockMap = stocks
            .GroupBy(x => x.OfferId)
            .ToDictionary(g => g.Key, g => g.First());
        var variantMap = variants.ToDictionary(x => x.VariantId, x => x.ProductId);

        return offers.Select(offer =>
        {
            variantMap.TryGetValue(offer.CatalogVariantId, out var productId);
            names.TryGetValue(productId, out var title);
            amountMap.TryGetValue(offer.OfferId, out var price);
            stockMap.TryGetValue(offer.OfferId, out var stock);
            var available = stock is null ? 0 : Math.Max(0, stock.OnHand - stock.Reserved);
            return new SellerOfferListItem(
                offer.OfferId,
                offer.CatalogVariantId,
                productId == Guid.Empty ? null : productId,
                string.IsNullOrWhiteSpace(title) ? "بدون عنوان" : title,
                offer.SellerSku,
                offer.Status.ToString(),
                price?.Amount,
                price?.Currency ?? "IRR",
                available,
                null);
        }).ToList();
    }

    private async Task<Dictionary<Guid, string>> ResolveTitlesAsync(
        IReadOnlyList<SellerOffer> offers,
        CancellationToken cancellationToken)
    {
        if (offers.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var presented = await PresentOffersAsync(offers, cancellationToken);
        return presented.ToDictionary(x => x.OfferId, x => x.ProductTitle);
    }
}
