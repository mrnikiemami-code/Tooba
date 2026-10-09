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
using Tooba.Offer.Contracts.ReturnPolicy;
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
using Tooba.Pricing.Contracts.Ports;
using Tooba.Promotion.Contracts.Checkout;
using Tooba.Promotion.Contracts.Merchandising;
using Tooba.Tax.Contracts.Ports;
namespace Tooba.Order.Infrastructure.Checkout.Persistence;

/// <summary>
/// ارکستراسیون checkout: سبد از قرارداد Cart، قیمت از Pricing، Offer از Lookup، رزرو از Inventory.
/// </summary>
public sealed partial class CheckoutDirectory : ICheckoutDirectory
{
    internal readonly OrderDbContext _db;
    internal readonly IOrderUseCaseGuard _guard;
    internal readonly ICartQueryGateway _carts;
    internal readonly IOfferLookupGateway _offers;
    internal readonly IPriceLookupGateway _prices;
    internal readonly IOrderInventoryLifecyclePort _inventoryLifecycle;
    internal readonly ITaxCalculator _taxes;
    internal readonly ICheckoutPromotionPort _promotions;
    internal readonly ICatalogCheckoutLookup _catalog;
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
    internal readonly IClock _clock;
    internal readonly IIdGenerator _ids;
    internal readonly ILogger<CheckoutProcessManager> _checkoutLogger;

    /// <summary>
    /// دایرکتوری را به schema order و درزهای ماژول‌های دیگر وصل می‌کند.
    /// </summary>
    public CheckoutDirectory(
        OrderDbContext db,
        IOrderUseCaseGuard guard,
        ICartQueryGateway carts,
        IOfferLookupGateway offers,
        IPriceLookupGateway prices,
        IOrderInventoryLifecyclePort inventoryLifecycle,
        ITaxCalculator taxes,
        ICheckoutPromotionPort promotions,
        ICatalogCheckoutLookup catalog,
        ISellerOrderCancelFulfillmentGate cancelFulfillmentGate,
        IClock clock,
        IIdGenerator ids,
        ILogger<CheckoutProcessManager> checkoutLogger,
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
        _offers = offers;
        _prices = prices;
        _inventoryLifecycle = inventoryLifecycle;
        _taxes = taxes;
        _promotions = promotions;
        _catalog = catalog;
        _cancelFulfillmentGate = cancelFulfillmentGate;
        _clock = clock;
        _ids = ids;
        _checkoutLogger = checkoutLogger;
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
        _cartConversion = cartConversion
            ?? throw new InvalidOperationException("درز تبدیل سبد checkout لازم است.");
    }

    /// <inheritdoc />
    public Task<CheckoutSnapshot> SubmitAsync(SubmitCheckoutCommand command, CancellationToken cancellationToken)
        => new CheckoutProcessManager(
            this,
            _inventoryReservation,
            _cartConversion,
            _clock,
            _ids,
            _checkoutLogger,
            _processes).SubmitAsync(command, cancellationToken);

    /// <inheritdoc />
    public async Task<CheckoutSnapshot> PreviewAsync(SubmitCheckoutCommand command, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var cart = await _carts.GetCartAsync(command.CartId, command.CartAccess, cancellationToken)
            ?? throw new ContractOperationException("checkout.cart.missing");
        if (cart.Status != CartStatus.Active)
        {
            throw new ContractOperationException("checkout.cart.expired");
        }

        if (cart.Lines.Count == 0)
        {
            throw new ContractOperationException("checkout.cart.empty");
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
            StorefrontCartCurrencyCompatibility.ResolveSoleCurrency(cart),
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
            ?? throw new ContractOperationException("order.cancel.forbidden");
        var group = await _db.Checkouts.SingleAsync(x => x.CheckoutId == order.CheckoutId, cancellationToken);
        EnsureAccess(group, access);

        var fulfillment = await _cancelFulfillmentGate.GetAsync(sellerOrderId, cancellationToken);
        if (!SellerOrderCancellationPolicy.CanCancel(order.Status, fulfillment))
        {
            throw new ContractOperationException("order.cancel.forbidden");
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
            ?? throw new ContractOperationException("order.restore.invalid_state");
        EnsureAccess(group, access);
        if (group.SellerOrders.Count == 0 || group.SellerOrders.Any(x => x.Status != SellerOrderStatus.Cancelled))
        {
            throw new ContractOperationException("order.restore.not_cancelled");
        }

        if (group.SellerOrders.Any(x => x.CancelledFromStatus is null))
        {
            throw new ContractOperationException("order.restore.missing_snapshot");
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
        catch (Exception)
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
            throw new ContractOperationException("order.operation.invalid");
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
    public async Task<CheckoutNoteDeleteOutcome> DeleteNoteAsync(
        Guid checkoutId,
        Guid noteId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var note = await _db.OperationalNotes
            .SingleOrDefaultAsync(x => x.CheckoutId == checkoutId && x.NoteId == noteId, cancellationToken);
        if (note is null)
        {
            return CheckoutNoteDeleteOutcome.NotFound;
        }

        var lockedByOther = await _db.AdminViewAcks.AsNoTracking()
            .AnyAsync(
                x => x.CheckoutId == checkoutId
                     && x.ViewerUserId != note.CreatedByUserId
                     && x.ViewedAt > note.CreatedAt,
                cancellationToken);
        if (lockedByOther)
        {
            return CheckoutNoteDeleteOutcome.Forbidden;
        }

        note.SoftDelete(actorUserId, DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        return CheckoutNoteDeleteOutcome.Deleted;
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

        _db.AdminViewAcks.Add(CheckoutAdminViewAck.Create(checkoutId, viewerUserId, _clock.UtcNow));
        await _db.SaveChangesAsync(cancellationToken);
    }
}
