using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Cart.Application;
using Tooba.Cart.Domain;
using Tooba.Catalog.Application;
using Tooba.Inventory.Application;
using Tooba.Offer.Application;
using Tooba.Offer.Domain;
using Tooba.Order.Application;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Pricing.Application;
using Tooba.Promotion.Application;
using Tooba.Promotion.Domain;
using Tooba.Tax.Application;
using Tooba.Tax.Domain;

namespace Tooba.Order.Infrastructure;

/// <summary>
/// نگهبان باز موردکاربرد Order. ماتریس هویت اینجا نیست.
/// </summary>
public sealed class OpenOrderUseCaseGuard : IOrderUseCaseGuard
{
    /// <inheritdoc />
    public Task EnsureCanMutateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>
/// ارکستراسیون checkout: سبد از قرارداد Cart، قیمت از Pricing، Offer از Lookup، رزرو از Inventory.
/// تراکنش توزیع‌شده نیست. نقل‌قول سبد حقیقت تسویه نیست؛ اختلاف قیمت با PRICE_CHANGED شکست می‌خورد.
/// </summary>
public sealed class CheckoutDirectory : ICheckoutDirectory
{
    private readonly OrderDbContext _db;
    private readonly IOrderUseCaseGuard _guard;
    private readonly ICartQueryGateway _carts;
    private readonly ICartDirectory _cartMutations;
    private readonly IOfferLookupGateway _offers;
    private readonly IPriceLookupGateway _prices;
    private readonly IInventoryDirectory _inventory;
    private readonly ITaxCalculator _taxes;
    private readonly IPromotionEvaluator _promotions;
    private readonly ICatalogLookupGateway _catalog;

    /// <summary>
    /// دایرکتوری را به schema order و درزهای ماژول‌های دیگر وصل می‌کند.
    /// </summary>
    public CheckoutDirectory(
        OrderDbContext db,
        IOrderUseCaseGuard guard,
        ICartQueryGateway carts,
        ICartDirectory cartMutations,
        IOfferLookupGateway offers,
        IPriceLookupGateway prices,
        IInventoryDirectory inventory,
        ITaxCalculator taxes,
        IPromotionEvaluator promotions,
        ICatalogLookupGateway catalog)
    {
        _db = db;
        _guard = guard;
        _carts = carts;
        _cartMutations = cartMutations;
        _offers = offers;
        _prices = prices;
        _inventory = inventory;
        _taxes = taxes;
        _promotions = promotions;
        _catalog = catalog;
    }

    /// <inheritdoc />
    public async Task<CheckoutSnapshot> SubmitAsync(SubmitCheckoutCommand command, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var existing = await FindCheckoutAsync(
            x => x.IdempotencyKey == command.IdempotencyKey.Trim() || x.CartId == command.CartId,
            cancellationToken);
        if (existing is not null)
        {
            EnsureAccess(existing, new OrderAccess(command.BuyerPartyId, command.PlacedByUserId));
            await ReconcileCartConversionAsync(existing, command, cancellationToken);
            return ToSnapshot(existing);
        }

        var cart = await _carts.GetCartAsync(command.CartId, command.CartAccess, cancellationToken)
            ?? throw new InvalidOperationException("سبد برای checkout پیدا نشد؛ CartId Bearer نیست.");
        if (cart.Status != CartStatus.Active)
        {
            throw new InvalidOperationException("فقط سبد Active به سفارش تبدیل می‌شود.");
        }

        if (cart.Version != command.ExpectedCartVersion)
        {
            throw new InvalidOperationException("نسخهٔ سبد کهنه است؛ checkout همزمان رد شد.");
        }

        if (cart.Lines.Count == 0)
        {
            throw new InvalidOperationException("سبد خالی به سفارش تبدیل نمی‌شود.");
        }

        var now = DateTimeOffset.UtcNow;
        var checkoutId = UuidV7.New();
        var sellerOrders = await QuoteSellerOrdersAsync(cart, command, checkoutId, now, cancellationToken);

        var group = CheckoutGroup.Submit(
            checkoutId,
            command.IdempotencyKey,
            cart.CartId,
            command.Mode,
            command.BuyerPartyId,
            command.PlacedByUserId,
            cart.Market,
            cart.Currency,
            cart.Channel,
            sellerOrders,
            now,
            command.RecipientName,
            command.ContactMobile,
            command.ProvinceName,
            command.CityName,
            command.PostalAddress,
            command.PostalCode,
            command.ShippingMethodCode,
            command.ShippingMethodLabel);
        _db.Checkouts.Add(group);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // بازندهٔ رقابت unique(cart_id) نباید موجودیت ردیابی‌شدهٔ خودش را برگرداند.
            _db.ChangeTracker.Clear();
            var winner = await _db.Checkouts
                .AsNoTracking()
                .Include(x => x.SellerOrders)
                .ThenInclude(x => x.Lines)
                .Where(x => x.CartId == command.CartId)
                .OrderBy(x => x.SubmittedAt)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException("checkout تکراری سبد ذخیره شد ولی خوانده نشد.");
            EnsureAccess(winner, new OrderAccess(command.BuyerPartyId, command.PlacedByUserId));
            await ReconcileCartConversionAsync(winner, command, cancellationToken);
            return ToSnapshot(winner);
        }

        await ReconcileCartConversionAsync(group, command, cancellationToken);
        return ToSnapshot(group);
    }

    /// <inheritdoc />
    public async Task<CheckoutSnapshot> PreviewAsync(SubmitCheckoutCommand command, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var cart = await _carts.GetCartAsync(command.CartId, command.CartAccess, cancellationToken)
            ?? throw new InvalidOperationException("سبد برای checkout پیدا نشد؛ CartId Bearer نیست.");
        if (cart.Status != CartStatus.Active)
        {
            throw new InvalidOperationException("فقط سبد Active به سفارش تبدیل می‌شود.");
        }

        if (cart.Lines.Count == 0)
        {
            throw new InvalidOperationException("سبد خالی به سفارش تبدیل نمی‌شود.");
        }

        var now = DateTimeOffset.UtcNow;
        var checkoutId = UuidV7.New();
        var sellerOrders = await QuoteSellerOrdersAsync(cart, command, checkoutId, now, cancellationToken);
        var group = CheckoutGroup.Submit(
            checkoutId,
            command.IdempotencyKey.Length == 0 ? "preview" : command.IdempotencyKey,
            cart.CartId,
            command.Mode,
            command.BuyerPartyId,
            command.PlacedByUserId,
            cart.Market,
            cart.Currency,
            cart.Channel,
            sellerOrders,
            now,
            command.RecipientName,
            command.ContactMobile,
            command.ProvinceName,
            command.CityName,
            command.PostalAddress,
            command.PostalCode,
            command.ShippingMethodCode,
            command.ShippingMethodLabel);
        return ToSnapshot(group);
    }

    /// <inheritdoc />
    public async Task<CheckoutSnapshot?> GetCheckoutAsync(Guid checkoutId, OrderAccess access, CancellationToken cancellationToken)
    {
        var group = await _db.Checkouts
            .Include(x => x.SellerOrders)
            .ThenInclude(x => x.Lines)
            .SingleOrDefaultAsync(x => x.CheckoutId == checkoutId, cancellationToken);
        if (group is null)
        {
            return null;
        }

        if (!group.CanBeViewedBy(access.BuyerPartyId, access.PlacedByUserId))
        {
            return null;
        }

        return ToSnapshot(group);
    }

    /// <inheritdoc />
    public async Task<SellerOrderSnapshot?> GetSellerOrderByNumberAsync(string orderNumber, OrderAccess access, CancellationToken cancellationToken)
    {
        var order = await _db.SellerOrders
            .Include(x => x.Lines)
            .SingleOrDefaultAsync(x => x.OrderNumber == orderNumber, cancellationToken);
        if (order is null)
        {
            return null;
        }

        var group = await _db.Checkouts.SingleAsync(x => x.CheckoutId == order.CheckoutId, cancellationToken);
        if (!group.CanBeViewedBy(access.BuyerPartyId, access.PlacedByUserId))
        {
            return null;
        }

        return ToSellerSnapshot(order);
    }

    /// <inheritdoc />
    public async Task CancelSellerOrderAsync(Guid sellerOrderId, OrderAccess access, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var order = await _db.SellerOrders
            .Include(x => x.Lines)
            .SingleOrDefaultAsync(x => x.SellerOrderId == sellerOrderId, cancellationToken)
            ?? throw new InvalidOperationException("سفارش فروشنده پیدا نشد.");
        var group = await _db.Checkouts.SingleAsync(x => x.CheckoutId == order.CheckoutId, cancellationToken);
        EnsureAccess(group, access);
        foreach (var line in order.Lines)
        {
            if (line.ReservationId is { } reservationId)
            {
                await _inventory.ReleaseAsync(reservationId, cancellationToken);
            }
        }

        order.Cancel();
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// قیمت، ترویج و مالیات را روی خطوط سبد دوباره ارزیابی می‌کند. نتیجه هنوز سفارش پایدار نیست.
    /// </summary>
    private async Task<List<SellerOrder>> QuoteSellerOrdersAsync(
        CartSnapshot cart,
        SubmitCheckoutCommand command,
        Guid checkoutId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var sellerOrders = new List<SellerOrder>();
        var sequence = 0;
        var categoryByVariant = await _catalog.GetPrimaryCategoryIdsByVariantIdsAsync(
            cart.Lines.Select(x => x.CatalogVariantId).Distinct().ToArray(),
            cancellationToken);
        foreach (var sellerGroup in cart.Lines.GroupBy(x => x.SellerPartyId))
        {
            sequence++;
            var sellerOrderId = UuidV7.New();
            var lines = new List<OrderLine>();
            foreach (var cartLine in sellerGroup)
            {
                var offer = await _offers.FindOfferAsync(cartLine.OfferId, cancellationToken)
                    ?? throw new InvalidOperationException("Offer از قرارداد Lookup پیدا نشد؛ DbContext Offer خوانده نشد.");
                if (offer.Status != OfferStatus.Active)
                {
                    throw new InvalidOperationException("Offer غیرفعال در checkout پذیرفته نمی‌شود.");
                }

                if (offer.SellerPartyId != cartLine.SellerPartyId)
                {
                    throw new InvalidOperationException("فروشندهٔ Offer با خط سبد یکی نیست.");
                }

                var quote = await _prices.ResolvePriceAsync(
                    new PriceResolutionQuery(
                        cartLine.OfferId,
                        cart.Market,
                        cart.Channel,
                        cart.Currency,
                        now,
                        command.BuyerPartyId,
                        null,
                        cartLine.Quantity),
                    cancellationToken)
                    ?? throw new InvalidOperationException("نقل‌قول قیمت از قرارداد Pricing پیدا نشد.");

                if (cartLine.QuotedAmount is null
                    || cartLine.QuotedAmount != quote.Amount
                    || !string.Equals(cartLine.QuotedCurrency, quote.Currency, StringComparison.Ordinal)
                    || cartLine.QuotedTaxExclusive != quote.TaxExclusive)
                {
                    throw new InvalidOperationException("PRICE_CHANGED");
                }

                if (cartLine.ReservationId is null)
                {
                    throw new InvalidOperationException("خط سبد بدون رزرو موجودی به سفارش تبدیل نمی‌شود.");
                }

                var lineExclusive = quote.Amount * cartLine.Quantity;
                var promotion = await _promotions.EvaluateAsync(
                    new PromotionEvaluationRequest(
                        cartLine.OfferId,
                        cartLine.CatalogVariantId,
                        null,
                        cartLine.SellerPartyId,
                        cart.Market,
                        cart.Channel.ToString(),
                        quote.Currency,
                        cartLine.Quantity,
                        lineExclusive,
                        command.BuyerPartyId,
                        null,
                        command.CouponCode,
                        now),
                    cancellationToken);

                var tax = await _taxes.CalculateAsync(
                    new TaxCalculationRequest(
                        cartLine.OfferId,
                        command.TaxJurisdiction,
                        cart.Market,
                        quote.Currency,
                        promotion.PostDiscountTaxExclusiveAmount,
                        1,
                        now,
                        command.BuyerPartyId,
                        AllowTrustedOverride: false,
                        TrustedOverrideRate: null),
                    cancellationToken);
                if (tax.Outcome is TaxOutcome.NoApplicableRule)
                {
                    throw new InvalidOperationException("TAX_NO_APPLICABLE_RULE");
                }

                if (tax.Outcome is TaxOutcome.CalculationError)
                {
                    throw new InvalidOperationException("TAX_CALCULATION_ERROR");
                }

                lines.Add(OrderLine.FromCheckout(
                    sellerOrderId,
                    cartLine.OfferId,
                    cartLine.CatalogVariantId,
                    cartLine.SellerPartyId,
                    cartLine.Quantity,
                    quote.Amount,
                    quote.Currency,
                    quote.TaxExclusive,
                    quote.PriceId,
                    cartLine.ReservationId,
                    tax.Outcome.ToString(),
                    tax.TaxRate,
                    tax.TaxAmount,
                    tax.TaxInclusiveAmount,
                    tax.RuleId,
                    promotion.DiscountAmount,
                    promotion.Applied.FirstOrDefault()?.PromotionId,
                    promotion.Applied.FirstOrDefault()?.Name,
                    promotion.Applied.FirstOrDefault()?.CouponCode,
                    promotion.Applied.FirstOrDefault()?.DiscountKind.ToString(),
                    lineExclusive,
                    promotion.PostDiscountTaxExclusiveAmount,
                    promotion.Applied.Count == 0 ? null : now,
                    categoryByVariant.GetValueOrDefault(cartLine.CatalogVariantId)));
            }

            sellerOrders.Add(SellerOrder.Open(
                checkoutId,
                sellerGroup.Key,
                BuildOrderNumber(now, sequence),
                command.Mode,
                cart.Currency,
                lines));
        }

        if (command.QuotedDiscountAmount is { } quotedDiscount
            && quotedDiscount != sellerOrders.Sum(x => x.DiscountSnapshot))
        {
            throw new InvalidOperationException("PROMOTION_CHANGED");
        }

        return sellerOrders;
    }

    /// <summary>
    /// checkout موجود را با کلید idempotency یا CartId پیدا می‌کند تا سبد یک‌بار بیشتر سفارش نشود.
    /// </summary>
    private Task<CheckoutGroup?> FindCheckoutAsync(
        Expression<Func<CheckoutGroup, bool>> predicate,
        CancellationToken cancellationToken) =>
        _db.Checkouts
            .Include(x => x.SellerOrders)
            .ThenInclude(x => x.Lines)
            .SingleOrDefaultAsync(predicate, cancellationToken);

    /// <summary>
    /// اگر Order ذخیره شده و Cart هنوز Active است، تبدیل سبد را بدون تراکنش توزیع‌شده و بدون قیمت‌گذاری دوباره تکرار می‌کند.
    /// شکست موقت تبدیل، checkout ذخیره‌شده را حذف نمی‌کند.
    /// </summary>
    private async Task ReconcileCartConversionAsync(
        CheckoutGroup group,
        SubmitCheckoutCommand command,
        CancellationToken cancellationToken)
    {
        var latest = await _carts.GetCartAsync(group.CartId, command.CartAccess, cancellationToken);
        if (latest is null || latest.Status == CartStatus.Converted)
        {
            return;
        }

        var intent = group.Mode == OrderMode.RequestToReserve
            ? CartConversionIntent.RequestToReserve
            : CartConversionIntent.OnlinePurchase;
        try
        {
            await _cartMutations.ConvertAsync(group.CartId, command.CartAccess, latest.Version, intent, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            // تبدیل سبد بعداً با همان checkout دوباره تلاش می‌شود؛ رزرو موجودی دوباره گرفته نمی‌شود.
        }
    }

    private static void EnsureAccess(CheckoutGroup group, OrderAccess access)
    {
        if (!group.CanBeViewedBy(access.BuyerPartyId, access.PlacedByUserId))
        {
            throw new InvalidOperationException("دسترسی به سفارش بدون هویت خریدار یا کاربر عامل رد شد؛ شمارهٔ سفارش Bearer نیست.");
        }
    }

    private static string BuildOrderNumber(DateTimeOffset now, int sequence) =>
        $"TB-{now.UtcDateTime:yyyyMMddHHmmss}-{sequence:D2}-{Guid.NewGuid().ToString("N")[..6]}";

    private static CheckoutSnapshot ToSnapshot(CheckoutGroup group) =>
        new(
            group.CheckoutId,
            group.CartId,
            group.Mode,
            group.BuyerPartyId,
            group.PlacedByUserId,
            group.Market,
            group.Currency,
            group.Channel,
            group.SubmittedAt,
            group.SellerOrders.Select(ToSellerSnapshot).ToList(),
            group.RecipientName,
            group.ContactMobile,
            group.ProvinceName,
            group.CityName,
            group.PostalAddress,
            group.PostalCode,
            group.ShippingMethodCode,
            group.ShippingMethodLabel);

    private static SellerOrderSnapshot ToSellerSnapshot(SellerOrder order) =>
        new(
            order.SellerOrderId,
            order.OrderNumber,
            order.SellerPartyId,
            order.Status,
            order.SubtotalSnapshot,
            order.TaxSnapshot,
            order.DiscountSnapshot,
            order.GrandTotalSnapshot,
            order.Currency,
            order.Lines.Select(line => new OrderLineSnapshot(
                line.LineId,
                line.OfferId,
                line.CatalogVariantId,
                line.SellerPartyId,
                line.Quantity,
                line.UnitPriceSnapshot,
                line.LineTotalSnapshot,
                line.Currency,
                line.TaxExclusive,
                line.PriceId,
                line.ReservationId,
                line.TaxOutcomeSnapshot,
                line.TaxRateSnapshot,
                line.TaxAmountSnapshot,
                line.TaxInclusiveSnapshot,
                line.TaxRuleIdSnapshot,
                line.DiscountAmountSnapshot,
                line.PromotionIdSnapshot,
                line.PromotionNameSnapshot,
                line.PromotionCodeSnapshot,
                line.DiscountKindSnapshot,
                line.PreDiscountTaxExclusiveSnapshot,
                line.PostDiscountTaxExclusiveSnapshot,
                line.PromotionAppliedAtSnapshot)).ToList());
}
