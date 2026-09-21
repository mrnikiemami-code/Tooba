using Tooba.Order.Contracts.Fulfillment;
using System.Linq.Expressions;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Cart.Application.Ports;
using Tooba.Cart.Application.Conversion;
using Tooba.Cart.Contracts;
using Tooba.Catalog.Application;
using Tooba.Inventory.Contracts.Availability;
using Tooba.Inventory.Contracts.Checkout;
using Tooba.Inventory.Contracts.Errors;
using Tooba.Inventory.Contracts.Orders;
using Tooba.Inventory.Contracts.Seller;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;
using Tooba.Order.Application;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Pricing.Contracts;
using Tooba.Promotion.Contracts.Checkout;
using Tooba.Promotion.Contracts.Merchandising;
using Tooba.Tax.Contracts;
namespace Tooba.Order.Infrastructure;

/// <summary>
/// ارکستراسیون checkout: سبد از قرارداد Cart، قیمت از Pricing، Offer از Lookup، رزرو از Inventory.
/// </summary>
public sealed partial class CheckoutDirectory : ICheckoutDirectory
{
    internal readonly OrderDbContext _db;
    internal readonly IOrderUseCaseGuard _guard;
    internal readonly ICartQueryGateway _carts;
    internal readonly ICartDirectory _cartMutations;
    internal readonly IOfferLookupGateway _offers;
    internal readonly IPriceLookupGateway _prices;
    internal readonly IOrderInventoryLifecyclePort _inventoryLifecycle;
    internal readonly ITaxCalculator _taxes;
    internal readonly ICheckoutPromotionPort _promotions;
    internal readonly ICatalogLookupGateway _catalog;
    internal readonly ISellerOrderCancelFulfillmentGate _cancelFulfillmentGate;
    internal readonly IReturnPolicyResolver _returnPolicies;
    internal readonly ICheckoutReservationHoldPolicy? _holdPolicy;
    internal readonly IReservationCycleDirectory? _cycles;
    internal readonly IReservationCyclePolicyResolver? _cyclePolicy;
    internal readonly ICheckoutCommitBarrier _commitBarrier;
    internal readonly ICheckoutAbuseGate _abuseGate;
    internal readonly ICampaignCartPriceAuthority? _campaignPrices;
    internal readonly ICheckoutProcessTracker? _processes;
    internal readonly ICheckoutInventoryReservationPort _inventoryReservation;
    internal readonly ICartConversionPort _cartConversion;

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
        IOrderInventoryLifecyclePort inventoryLifecycle,
        ITaxCalculator taxes,
        ICheckoutPromotionPort promotions,
        ICatalogLookupGateway catalog,
        ISellerOrderCancelFulfillmentGate cancelFulfillmentGate,
        IReturnPolicyResolver? returnPolicies = null,
        ICheckoutReservationHoldPolicy? holdPolicy = null,
        IReservationCycleDirectory? cycles = null,
        IReservationCyclePolicyResolver? cyclePolicy = null,
        ICheckoutCommitBarrier? commitBarrier = null,
        ICheckoutAbuseGate? abuseGate = null,
        ICampaignCartPriceAuthority? campaignPrices = null,
        ICheckoutProcessTracker? processes = null,
        ICheckoutInventoryReservationPort? inventoryReservation = null,
        ICartConversionPort? cartConversion = null)
    {
        _db = db;
        _guard = guard;
        _carts = carts;
        _cartMutations = cartMutations;
        _offers = offers;
        _prices = prices;
        _inventoryLifecycle = inventoryLifecycle;
        _taxes = taxes;
        _promotions = promotions;
        _catalog = catalog;
        _cancelFulfillmentGate = cancelFulfillmentGate;
        _returnPolicies = returnPolicies ?? new ReturnPolicyResolver(new ReturnPolicyOptions());
        _holdPolicy = holdPolicy;
        _cycles = cycles;
        _cyclePolicy = cyclePolicy;
        _commitBarrier = commitBarrier ?? new NullCheckoutCommitBarrier();
        _abuseGate = abuseGate ?? new NullCheckoutAbuseGate();
        _campaignPrices = campaignPrices;
        _processes = processes;
        _inventoryReservation = inventoryReservation
            ?? throw new InvalidOperationException("درز رزرو موجودی checkout لازم است.");
        _cartConversion = cartConversion ?? new CartConversionAdapter(_cartMutations);
    }

    /// <inheritdoc />
    public Task<CheckoutSnapshot> SubmitAsync(SubmitCheckoutCommand command, CancellationToken cancellationToken)
        => new CheckoutProcessManager(this, _inventoryReservation, _cartConversion, _processes).SubmitAsync(command, cancellationToken);

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
        var sellerOrders = await QuoteSellerOrdersAsync(
            cart,
            command,
            checkoutId,
            now,
            new Dictionary<Guid, Guid>(),
            requireReservation: false,
            cancellationToken);
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
            command.ShippingMethodLabel,
            command.ShippingAmount,
            command.MinimumDeliveryDate,
            command.RequestedDeliveryDate,
            command.RequestedDeliveryTimeWindow,
            command.CustomerNote,
            command.RecipientFirstName,
            command.RecipientLastName);
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

        var fulfillment = await _cancelFulfillmentGate.GetAsync(sellerOrderId, cancellationToken);
        if (!SellerOrderCancellationPolicy.CanCancel(order.Status, fulfillment))
        {
            throw new InvalidOperationException(
                "order.cancel.forbidden: لغو از این وضعیت سفارش/ارسال مجاز نیست.");
        }

        foreach (var line in order.Lines)
        {
            if (line.ReservationId is { } reservationId)
            {
                await _inventoryLifecycle.ReleaseHeldReservationAsync(reservationId, cancellationToken);
            }
        }

        if (_cycles is not null
            && group.SellerOrders.Where(x => x.SellerOrderId != sellerOrderId)
                .All(x => x.Status == SellerOrderStatus.Cancelled))
        {
            await _cycles.CloseActiveAsync(
                group.CheckoutId,
                ReservationCycleStatus.ReleasedByCancel,
                DateTimeOffset.UtcNow,
                cancellationToken);
        }

        if (SellerOrderCancellationPolicy.IsOpenCancellable(order.Status))
        {
            order.Cancel();
        }
        else
        {
            order.CancelPaidBeforeShipment();
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task RestoreCancelledCheckoutAsync(Guid checkoutId, OrderAccess access, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var group = await _db.Checkouts
            .Include(x => x.SellerOrders)
            .ThenInclude(x => x.Lines)
            .SingleOrDefaultAsync(x => x.CheckoutId == checkoutId, cancellationToken)
            ?? throw new InvalidOperationException("سفارش پیدا نشد.");
        EnsureAccess(group, access);
        if (group.SellerOrders.Count == 0 || group.SellerOrders.Any(x => x.Status != SellerOrderStatus.Cancelled))
        {
            throw new InvalidOperationException("order.restore.not_cancelled");
        }

        if (group.SellerOrders.Any(x => x.CancelledFromStatus is null))
        {
            throw new InvalidOperationException("order.restore.missing_snapshot");
        }

        var acquired = new List<Guid>();
        try
        {
            foreach (var order in group.SellerOrders)
            {
                foreach (var line in order.Lines)
                {
                    if (line.ReservationId is not { } previousId)
                    {
                        continue;
                    }

                    // Restore reacquires a durable hold (expiresAt: null) — not cart TTL.
                    // Paid/pending restored reservations must not be eligible for ReleaseExpiredHoldsAsync.
                    var newReservationId = await _inventoryLifecycle.ReacquireDurableHoldFromPreviousAsync(
                        previousId,
                        $"order-restore-{line.LineId:N}",
                        $"order-restore-{line.LineId:N}",
                        cancellationToken);
                    acquired.Add(newReservationId);
                    line.ReplaceReservation(newReservationId);
                }

                order.RestoreFromCancellation(DateTimeOffset.UtcNow);
            }

            await _db.SaveChangesAsync(cancellationToken);
            if (_cycles is not null)
            {
                var policy = await ResolveCyclePolicyAsync(
                    group.SellerOrders.SelectMany(x => x.Lines)
                        .Select(x => new ReservationCyclePolicyLine(x.OfferId, x.CategoryIdSnapshot))
                        .ToArray(),
                    cancellationToken);
                var reservationIds = acquired.Count > 0
                    ? acquired
                    : group.SellerOrders.SelectMany(x => x.Lines)
                        .Where(x => x.ReservationId is not null)
                        .Select(x => x.ReservationId!.Value)
                        .ToList();
                await _cycles.StartAsync(
                    checkoutId,
                    ReservationCycleReason.Restore,
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow.AddMinutes(Math.Max(1, policy.InitialHoldMinutes)),
                    policy,
                    reservationIds,
                    actor: "restore",
                    correlationId: $"cycle-restore:{checkoutId:N}",
                    paymentAttemptId: null,
                    cancellationToken);
                await _cycles.CloseActiveAsync(
                    checkoutId,
                    ReservationCycleStatus.CommittedPaid,
                    DateTimeOffset.UtcNow,
                    cancellationToken);
            }
        }
        catch
        {
            foreach (var reservationId in acquired)
            {
                // رزرو تازه‌گرفته‌شده را تا حد ممکن آزاد می‌کنیم؛ سفارش لغو می‌ماند.
                await _inventoryLifecycle.TryReleaseReservationAsync(reservationId, cancellationToken);
            }

            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CheckoutOperationalNoteSnapshot>> ListNotesAsync(
        Guid checkoutId,
        Guid viewerUserId,
        int take,
        CancellationToken cancellationToken)
    {
        var bound = Math.Clamp(take <= 0 ? 50 : take, 1, 50);
        var exists = await _db.Checkouts.AsNoTracking()
            .AnyAsync(x => x.CheckoutId == checkoutId, cancellationToken);
        if (!exists)
        {
            return Array.Empty<CheckoutOperationalNoteSnapshot>();
        }

        var notes = await _db.OperationalNotes.AsNoTracking()
            .Where(x => x.CheckoutId == checkoutId && x.DeletedAt == null)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.NoteId)
            .Take(bound)
            .ToListAsync(cancellationToken);
        if (notes.Count == 0)
        {
            return Array.Empty<CheckoutOperationalNoteSnapshot>();
        }

        var earliest = notes.Min(x => x.CreatedAt);
        var otherViews = await _db.AdminViewAcks.AsNoTracking()
            .Where(x => x.CheckoutId == checkoutId && x.ViewedAt >= earliest)
            .Select(x => new { x.ViewerUserId, x.ViewedAt })
            .ToListAsync(cancellationToken);

        return notes.Select(note =>
        {
            var lockedByOther = otherViews.Any(v =>
                v.ViewerUserId != note.CreatedByUserId && v.ViewedAt > note.CreatedAt);
            var canDelete = note.CreatedByUserId == viewerUserId && !lockedByOther;
            return new CheckoutOperationalNoteSnapshot(
                note.NoteId,
                note.CheckoutId,
                note.Body,
                note.CreatedByUserId,
                note.CreatedAt,
                canDelete);
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<CheckoutOperationalNoteSnapshot> AddNoteAsync(
        Guid checkoutId,
        Guid actorUserId,
        string body,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var exists = await _db.Checkouts.AsNoTracking()
            .AnyAsync(x => x.CheckoutId == checkoutId, cancellationToken);
        if (!exists)
        {
            throw new InvalidOperationException("سفارش پیدا نشد.");
        }

        var note = CheckoutOperationalNote.Create(checkoutId, actorUserId, body, DateTimeOffset.UtcNow);
        _db.OperationalNotes.Add(note);
        await _db.SaveChangesAsync(cancellationToken);
        return new CheckoutOperationalNoteSnapshot(
            note.NoteId,
            note.CheckoutId,
            note.Body,
            note.CreatedByUserId,
            note.CreatedAt,
            CanDelete: true);
    }

    /// <inheritdoc />
    public async Task DeleteNoteAsync(
        Guid checkoutId,
        Guid noteId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var note = await _db.OperationalNotes
            .SingleOrDefaultAsync(x => x.CheckoutId == checkoutId && x.NoteId == noteId, cancellationToken)
            ?? throw new InvalidOperationException("یادداشت پیدا نشد.");

        var lockedByOther = await _db.AdminViewAcks.AsNoTracking()
            .AnyAsync(
                x => x.CheckoutId == checkoutId
                     && x.ViewerUserId != note.CreatedByUserId
                     && x.ViewedAt > note.CreatedAt,
                cancellationToken);
        if (lockedByOther)
        {
            throw new InvalidOperationException("حذف یادداشت پس از مشاهدهٔ کاربر دیگر مجاز نیست.");
        }

        note.SoftDelete(actorUserId, DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task RecordAdminViewAsync(
        Guid checkoutId,
        Guid viewerUserId,
        CancellationToken cancellationToken)
    {
        var exists = await _db.Checkouts.AsNoTracking()
            .AnyAsync(x => x.CheckoutId == checkoutId, cancellationToken);
        if (!exists || viewerUserId == Guid.Empty)
        {
            return;
        }

        _db.AdminViewAcks.Add(CheckoutAdminViewAck.Create(checkoutId, viewerUserId, DateTimeOffset.UtcNow));
        await _db.SaveChangesAsync(cancellationToken);
    }

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
        var moneyPlaces = FinancialRounder.MoneyPlaces(cart.Currency);
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

                var quote = await ResolveCheckoutLineQuoteAsync(cart, cartLine, now, cancellationToken)
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
                cart.Currency,
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
            await _cartMutations.ConvertAsync(group.CartId, command.CartAccess, latest.Version, intent, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            // تبدیل سبد بعداً با همان checkout دوباره تلاش می‌شود؛ رزرو موجودی دوباره گرفته نمی‌شود.
        }
    }

    internal static void EnsureAccess(CheckoutGroup group, OrderAccess access)
    {
        if (!group.CanBeViewedBy(access.BuyerPartyId, access.PlacedByUserId))
        {
            throw new InvalidOperationException("دسترسی به سفارش بدون هویت خریدار یا کاربر عامل رد شد؛ شمارهٔ سفارش Bearer نیست.");
        }
    }

    private static string BuildOrderNumber(DateTimeOffset now, int sequence) =>
        $"TB-{now.UtcDateTime:yyyyMMddHHmmss}-{sequence:D2}-{Guid.NewGuid().ToString("N")[..6]}";

    internal static CheckoutSnapshot ToSnapshot(CheckoutGroup group) =>
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
            group.ShippingMethodLabel,
            group.ShippingAmount,
            group.MinimumDeliveryDate,
            group.RequestedDeliveryDate,
            group.RequestedDeliveryTimeWindow,
            group.CustomerNote,
            group.RecipientFirstName,
            group.RecipientLastName);

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
                line.PromotionAppliedAtSnapshot,
                line.UnitOfMeasureIdSnapshot,
                line.UnitCodeSnapshot,
                line.UnitDisplaySnapshot,
                line.QuantityDecimalPlacesSnapshot,
                line.QuantityStepSnapshot)).ToList());

    /// <summary>
    /// نقل‌قول خط checkout: کمپین واجد شرایط در صورت وجود، وگرنه Base.
    /// </summary>
    private async Task<PriceQuote?> ResolveCheckoutLineQuoteAsync(
        CartSnapshot cart,
        CartLineSnapshot cartLine,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (cartLine.MerchandisingCampaignId is Guid campaignId
            && campaignId != Guid.Empty
            && _campaignPrices is not null)
        {
            var campaignQuote = await _campaignPrices.TryResolveEligibleCampaignPriceAsync(
                campaignId,
                cartLine.OfferId,
                cart.Market,
                cart.Channel,
                cart.Currency,
                now,
                cancellationToken);
            if (campaignQuote is not null)
            {
                return campaignQuote;
            }
        }

        return await _prices.ResolvePriceAsync(
            new PriceResolutionQuery(
                cartLine.OfferId,
                cart.Market,
                cart.Channel,
                cart.Currency,
                now,
                null,
                null,
                cartLine.Quantity),
            cancellationToken);
    }
}
