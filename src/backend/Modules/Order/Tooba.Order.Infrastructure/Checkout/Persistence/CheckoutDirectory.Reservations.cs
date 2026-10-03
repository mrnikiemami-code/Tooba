using Tooba.Order.Contracts.Fulfillment;
using System.Linq.Expressions;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tooba.BuildingBlocks;
using Tooba.Cart.Contracts;
using Tooba.Catalog.Contracts.Checkout;
using Tooba.Inventory.Contracts.Availability;
using Tooba.Inventory.Contracts.Checkout;
using Tooba.Inventory.Contracts.Errors;
using Tooba.Inventory.Contracts.Orders;
using Tooba.Inventory.Contracts.Seller;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;
using Tooba.Order.Application;
using Tooba.Order.Application.Checkout.Abuse;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Checkout.Policies;
using Tooba.Order.Application.Checkout.Process;
using Tooba.Order.Application.ReservationCycle.Contracts;
using Tooba.Order.Application.ReservationCycle.Policies;
using Tooba.Order.Application.ReservationCycle.Services;
using Tooba.Order.Application.Seller.Policies;
using Tooba.Order.Application.Storefront.Services;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Pricing.Contracts;
using Tooba.Promotion.Contracts.Checkout;
using Tooba.Promotion.Contracts.Merchandising;
using Tooba.Tax.Contracts;
namespace Tooba.Order.Infrastructure.Checkout.Persistence;

public sealed partial class CheckoutDirectory : ICheckoutDirectory
{
    internal async Task<Dictionary<Guid, Guid>> ReserveCartLinesForOrderAsync(
        CartSnapshot cart,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var expiresAt = await ResolveInitialCycleExpiresAtAsync(cart, now, cancellationToken);
        var result = await _inventoryReservation.ReserveForCheckoutAsync(
            new CheckoutInventoryReservationRequest(
                cart.CartId,
                ProcessId: null,
                CorrelationId: $"checkout-submit:{cart.CartId:N}",
                now,
                expiresAt,
                cart.Lines.Select(line => new CheckoutInventoryLineRequest(
                    line.LineId,
                    line.OfferId,
                    line.Quantity,
                    line.ReservationId)).ToArray()),
            cancellationToken);
        return result.ByCartLineId.ToDictionary(x => x.Key, x => x.Value);
    }

    internal static void BindReservationsToOrders(
        CheckoutGroup group,
        CartSnapshot cart,
        IReadOnlyDictionary<Guid, Guid> reservationsByCartLineId)
    {
        var unused = group.SellerOrders.SelectMany(o => o.Lines).ToList();
        foreach (var cartLine in cart.Lines)
        {
            if (!reservationsByCartLineId.TryGetValue(cartLine.LineId, out var reservationId))
            {
                throw new InvalidOperationException("inventory.supply.unavailable");
            }

            var orderLine = unused.FirstOrDefault(x =>
                x.OfferId == cartLine.OfferId
                && x.SellerPartyId == cartLine.SellerPartyId
                && x.Quantity == cartLine.Quantity
                && x.ReservationId is null)
                ?? throw new InvalidOperationException("inventory.supply.unavailable");
            orderLine.ReplaceReservation(reservationId);
            unused.Remove(orderLine);
        }
    }

    internal async Task PrepareInitialCycleAsync(
        CheckoutGroup group,
        OrderMode mode,
        IEnumerable<Guid> reservationIds,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (_cycles is null)
        {
            return;
        }

        var policy = await ResolveCyclePolicyAsync(
            group.SellerOrders.SelectMany(x => x.Lines)
                .Select(x => new ReservationCyclePolicyLine(x.OfferId, x.CategoryIdSnapshot))
                .ToArray(),
            cancellationToken);
        _cycles.PrepareStart(
            group.CheckoutId,
            mode == OrderMode.OnlinePurchase
                ? ReservationCycleReason.InitialPayment
                : ReservationCycleReason.ManualInitial,
            now,
            now.AddMinutes(policy.InitialHoldMinutes),
            policy,
            reservationIds.Distinct().ToArray(),
            actor: "order-commit",
            correlationId: $"cycle:{group.CheckoutId:N}:1",
            paymentAttemptId: null);
    }

    private async Task<DateTimeOffset> ResolveInitialCycleExpiresAtAsync(
        CartSnapshot cart,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (_cyclePolicy is not null)
        {
            var categoryByVariant = await _catalog.GetPrimaryCategoryIdsByVariantIdsAsync(
                cart.Lines.Select(x => x.CatalogVariantId).Distinct().ToArray(),
                cancellationToken);
            var policy = await ResolveCyclePolicyAsync(
                cart.Lines.Select(x => new ReservationCyclePolicyLine(
                    x.OfferId,
                    categoryByVariant.GetValueOrDefault(x.CatalogVariantId))).ToArray(),
                cancellationToken);
            return now.AddMinutes(policy.InitialHoldMinutes);
        }

        return _holdPolicy?.ResolveInitialExpiresAt(now) ?? now.AddHours(2);
    }

    private async Task<ReservationCyclePolicySnapshot> ResolveCyclePolicyAsync(
        IReadOnlyList<ReservationCyclePolicyLine> lines,
        CancellationToken cancellationToken)
    {
        if (_cyclePolicy is not null)
        {
            return await _cyclePolicy.ResolveAsync(lines, cancellationToken);
        }

        return new ReservationCyclePolicySnapshot(120, 120, 3, "platform");
    }

    internal Task ReleaseAcquiredAsync(IEnumerable<Guid> reservationIds, CancellationToken cancellationToken)
        => _inventoryReservation.ReleaseAsync(reservationIds, cancellationToken);

    /// <summary>
    /// قیمت، ترویج و مالیات را روی خطوط سبد دوباره ارزیابی می‌کند. نتیجه هنوز سفارش پایدار نیست.
    /// </summary>
    internal async Task<List<SellerOrder>> QuoteSellerOrdersAsync(
        CartSnapshot cart,
        SubmitCheckoutCommand command,
        Guid checkoutId,
        DateTimeOffset now,
        IReadOnlyDictionary<Guid, Guid> reservationsByCartLineId,
        bool requireReservation,
        CancellationToken cancellationToken)
    {
        var sellerOrders = new List<SellerOrder>();
        var sequence = 0;
        var categoryByVariant = await _catalog.GetPrimaryCategoryIdsByVariantIdsAsync(
            cart.Lines.Select(x => x.CatalogVariantId).Distinct().ToArray(),
            cancellationToken);
        var quantityPolicies = await _catalog.GetEffectiveQuantityPoliciesForVariantIdsAsync(
            cart.Lines.Select(x => x.CatalogVariantId).Distinct().ToArray(),
            cancellationToken);
        var rounding = await _catalog.GetGlobalRoundingModeAsync(cancellationToken);
        var effectiveCurrency = StorefrontCartCurrencyCompatibility.ResolveSoleCurrency(cart);
        var moneyPlaces = FinancialRounder.MoneyPlaces(effectiveCurrency);
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

                var returnPolicy = _returnPolicies.ResolveForCheckout(
                    offer.ReturnPolicyChoice,
                    offer.CustomReturnWindowDays);

                var quote = await ResolveCheckoutLineQuoteAsync(cart, cartLine, effectiveCurrency, now, cancellationToken)
                    ?? throw new InvalidOperationException("نقل‌قول قیمت از قرارداد Pricing پیدا نشد.");

                if (cartLine.QuotedAmount is null
                    || cartLine.QuotedAmount != quote.Amount
                    || !string.Equals(cartLine.QuotedCurrency, quote.Currency, StringComparison.Ordinal)
                    || cartLine.QuotedTaxExclusive != quote.TaxExclusive)
                {
                    throw new InvalidOperationException("PRICE_CHANGED");
                }

                reservationsByCartLineId.TryGetValue(cartLine.LineId, out var reservedId);
                var reservationId = reservedId != Guid.Empty ? reservedId : cartLine.ReservationId;
                if (requireReservation && reservationId is null)
                {
                    throw new InvalidOperationException("inventory.supply.unavailable");
                }

                var lineExclusive = quote.Amount * cartLine.Quantity;
                var promotion = await _promotions.EvaluateForCheckoutAsync(
                    new CheckoutPromotionEvaluationRequest(
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
                        now,
                        rounding),
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
                        TrustedOverrideRate: null,
                        rounding),
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
                    reservationId,
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
                    categoryByVariant.GetValueOrDefault(cartLine.CatalogVariantId),
                    returnPolicy.IsReturnable,
                    returnPolicy.WindowDays,
                    returnPolicy.Source,
                    returnPolicy.LabelFa,
                    quantityPolicies.GetValueOrDefault(cartLine.CatalogVariantId)?.UnitOfMeasureId,
                    quantityPolicies.GetValueOrDefault(cartLine.CatalogVariantId)?.UnitCode,
                    quantityPolicies.GetValueOrDefault(cartLine.CatalogVariantId)?.UnitDisplayName,
                    quantityPolicies.GetValueOrDefault(cartLine.CatalogVariantId)?.DecimalPlaces ?? 0,
                    quantityPolicies.GetValueOrDefault(cartLine.CatalogVariantId)?.Step));
            }

            sellerOrders.Add(SellerOrder.Open(
                checkoutId,
                sellerGroup.Key,
                BuildOrderNumber(now, sequence),
                command.Mode,
                effectiveCurrency,
                lines,
                rounding,
                moneyPlaces));
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
    internal Task<CheckoutGroup?> FindCheckoutAsync(
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
    internal async Task ReconcileCartConversionAsync(
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
            await _cartConversion.ConvertForCheckoutAsync(
                new CartConversionRequest(
                    group.CartId,
                    command.CartAccess,
                    latest.Version,
                    intent,
                    null,
                    $"checkout-reconcile:{group.CheckoutId:N}"),
                cancellationToken);
        }
        catch (InvalidOperationException)
        {
            // تبدیل سبد بعداً با همان checkout دوباره تلاش می‌شود؛ رزرو موجودی دوباره گرفته نمی‌شود.
        }
    }
}
