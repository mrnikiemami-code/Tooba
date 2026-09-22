using Tooba.BuildingBlocks;
using Tooba.Cart.Contracts;
using Tooba.Catalog.Contracts;
using Tooba.Fulfillment.Contracts.Operations;
using Tooba.Order.Application.Admin.Operations.Policies;
using Tooba.Order.Application.Storefront.Models;
using Tooba.Order.Application.Storefront.Ports;
using Tooba.Order.Domain;
using Tooba.Payment.Contracts.Storefront;
using Tooba.Settlement.Contracts.Operations;

namespace Tooba.Order.Application.Storefront.Services;

/// <summary>
/// Order-owned storefront pending payment list/cancel/hide.
/// </summary>
public sealed class StorefrontPendingPaymentService
{
    private readonly IStorefrontPendingCheckoutStore _store;
    private readonly IPendingPaymentReader _payments;
    private readonly ICatalogCartPresentationLookup _catalog;
    private readonly ICartPresentationGateway _carts;
    private readonly IReservationCycleDirectory _cycles;
    private readonly ICheckoutDirectory _checkout;
    private readonly IFulfillmentAdminOperations _fulfillment;
    private readonly ISettlementOrderAccrualPort _settlement;
    private readonly IOrderStorefrontActor _actor;
    private readonly IClock _clock;

    public StorefrontPendingPaymentService(
        IStorefrontPendingCheckoutStore store,
        IPendingPaymentReader payments,
        ICatalogCartPresentationLookup catalog,
        ICartPresentationGateway carts,
        IReservationCycleDirectory cycles,
        ICheckoutDirectory checkout,
        IFulfillmentAdminOperations fulfillment,
        ISettlementOrderAccrualPort settlement,
        IOrderStorefrontActor actor,
        IClock clock)
    {
        _store = store;
        _payments = payments;
        _catalog = catalog;
        _carts = carts;
        _cycles = cycles;
        _checkout = checkout;
        _fulfillment = fulfillment;
        _settlement = settlement;
        _actor = actor;
        _clock = clock;
    }
    /// <summary>فهرست سفارش‌های unpaid قابل‌اقدام یا اطلاع‌رسانی را برمی‌گرداند.</summary>
    public async Task<StorefrontPendingPaymentPage> ListAsync(
        StorefrontPendingPaymentQueryRequest? query,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var groups = await ResolveOwnedGroupsAsync(query, cancellationToken);
        if (groups.Count == 0)
        {
            return new StorefrontPendingPaymentPage(now, []);
        }

        var checkoutIds = groups.Select(x => x.CheckoutId).ToArray();
        var paymentRows = await _payments.GetLatestByCheckoutIdsAsync(checkoutIds, cancellationToken);
        var latestPayments = paymentRows.ToDictionary(
            row => row.CheckoutId,
            row => new StorefrontPendingPaymentProjector.PaymentInput(row.PaymentId, row.Status, row.ProviderCode,
                row.EvidenceSubmittedAt,
                row.Amount,
                row.Currency));
        var cycleMap = await _cycles.GetProjectionsAsync(checkoutIds, now, null, cancellationToken);
        var lineCatalog = await LoadLineCatalogAsync(groups, cancellationToken);
        var inputs = groups.Select(group => new StorefrontPendingPaymentProjector.CheckoutInput(
            group.CheckoutId,
            group.CartId,
            group.PlacedByUserId,
            group.SubmittedAt,
            group.SellerOrders.Select(order => new StorefrontPendingPaymentProjector.SellerInput(
                order.OrderNumber,
                order.Status,
                order.GrandTotalSnapshot,
                order.Currency,
                order.Lines.Select(line =>
                {
                    lineCatalog.TryGetValue(line.CatalogVariantId, out var catalog);
                    return new StorefrontPendingPaymentProjector.LineInput(
                        catalog.Title,
                        line.Quantity,
                        catalog.MediaAssetId);
                }).ToArray())).ToArray())).ToArray();
        var page = StorefrontPendingPaymentProjector.Project(inputs, latestPayments, cycleMap, now);
        var hidden = await LoadHiddenCheckoutIdsAsync(groups, cancellationToken);
        if (hidden.Count == 0)
        {
            return page;
        }

        return new StorefrontPendingPaymentPage(
            page.ServerTime,
            page.Items.Where(x => !hidden.Contains(x.CheckoutId)).ToList());
    }

    /// <summary>
    /// پنهان‌کردن کارت pending فقط ترجیح نمایش است؛ سفارش/پرداخت/رزرو را تغییر نمی‌دهد.
    /// </summary>
    public async Task<object> HidePendingCardAsync(
        Guid checkoutId,
        Guid cartId,
        string? guestSecret,
        CancellationToken cancellationToken)
    {
        var group = await ResolveOwnedGroupForCancelAsync(checkoutId, cartId, guestSecret, cancellationToken);
        var now = _clock.UtcNow;
        var cycle = (await _cycles.GetProjectionsAsync([group.CheckoutId], now, null, cancellationToken))
            .GetValueOrDefault(group.CheckoutId);
        if (cycle is { CurrentStatus: ReservationCycleStatus.Active, SecondsRemaining: > 0 })
        {
            throw new StorefrontOrderException(StorefrontOrderErrors.PendingHideActiveHold);
        }

        var actor = _actor.TryResolveListActor();
        var ownerUserId = actor;
        var guestCartId = actor is null ? group.CartId : (Guid?)null;
        await _store.HidePendingCardAsync(group.CheckoutId, ownerUserId, guestCartId, now, cancellationToken);

        return new { ok = true, checkoutId = group.CheckoutId, hidden = true };
    }

    /// <summary>
    /// لغو سفارش unpaid توسط مشتری/مهمان مالک. رزرو را آزاد می‌کند و سفارش را Cancelled می‌کند.
    /// </summary>
    public async Task<object> CancelAsync(
        Guid checkoutId,
        Guid cartId,
        string? guestSecret,
        CancellationToken cancellationToken)
    {
        var group = await ResolveOwnedGroupForCancelAsync(checkoutId, cartId, guestSecret, cancellationToken);
        if (group.SellerOrders.Count == 0)
        {
            throw new StorefrontOrderException(StorefrontOrderErrors.PaymentMissing);
        }

        if (AdminOrderOperationsPolicy.IsCheckoutCancelled(group.SellerOrders.Select(x => x.Status)))
        {
            return new { ok = true, checkoutId = group.CheckoutId, alreadyCancelled = true };
        }

        if (group.SellerOrders.Any(x => x.Status == SellerOrderStatus.Paid))
        {
            throw new StorefrontOrderException(StorefrontOrderErrors.OrderCancelUnpaidOnly);
        }

        var payment = await _payments.GetLatestOperationalForCheckoutAsync(group.CheckoutId, cancellationToken);
        if (payment is not null
            && payment.Status is "Succeeded" or "RefundPending" or "Refunded" or "RefundFailed")
        {
            throw new StorefrontOrderException(StorefrontOrderErrors.OrderCancelUnpaidOnly);
        }

        var fulfillments = await _fulfillment.ListForCheckoutAsync(group.CheckoutId, cancellationToken);
        if (AdminOrderOperationsPolicy.HasDispatchedOrDelivered(fulfillments))
        {
            throw new StorefrontOrderException(StorefrontOrderErrors.OrderCancelForbidden);
        }

        await _fulfillment.AbortForCheckoutCancelAsync(group.CheckoutId, cancellationToken);

        var access = new OrderAccess(group.BuyerPartyId, group.PlacedByUserId);
        var cancelled = new List<Guid>();
        foreach (var order in group.SellerOrders)
        {
            if (order.Status == SellerOrderStatus.Cancelled)
            {
                continue;
            }

            await _checkout.CancelSellerOrderAsync(order.SellerOrderId, access, cancellationToken);
            cancelled.Add(order.SellerOrderId);
        }

        if (cancelled.Count == 0)
        {
            throw new StorefrontOrderException(StorefrontOrderErrors.OrderCancelForbidden);
        }

        if (payment is not null)
        {
            await _settlement.NeutralizeUnpaidAccrualForCancelAsync(
                payment.PaymentId,
                group.SellerOrders.Select(x => x.SellerOrderId).ToList(),
                cancellationToken);
        }

        await _payments.CloseOrStartRefundForOrderCancelAsync(group.CheckoutId, cancellationToken);
        return new { ok = true, checkoutId = group.CheckoutId, alreadyCancelled = false };
    }

    private async Task<CheckoutGroup> ResolveOwnedGroupForCancelAsync(
        Guid checkoutId,
        Guid cartId,
        string? guestSecret,
        CancellationToken cancellationToken)
    {
        var group = await _store.GetWithSellerOrdersAsync(checkoutId, cancellationToken) ?? throw new StorefrontOrderException(StorefrontOrderErrors.PaymentMissing);

        var actor = _actor.TryResolveListActor();
        if (actor is Guid userId)
        {
            if (group.PlacedByUserId != userId)
            {
                throw new StorefrontOrderException(StorefrontOrderErrors.CheckoutAccessDenied);
            }

            if (cartId != Guid.Empty && cartId != group.CartId)
            {
                throw new StorefrontOrderException(StorefrontOrderErrors.CheckoutAccessDenied);
            }

            return group;
        }

        if (cartId == Guid.Empty || cartId != group.CartId)
        {
            throw new StorefrontOrderException(StorefrontOrderErrors.CheckoutAccessDenied);
        }

        var owned = await _carts.TryGetForOwnershipAsync(group.CartId, guestSecret, cancellationToken);
        if (owned is null)
        {
            throw new StorefrontOrderException(StorefrontOrderErrors.CheckoutAccessDenied);
        }

        return group;
    }

    private async Task<IReadOnlyList<CheckoutGroup>> ResolveOwnedGroupsAsync(
        StorefrontPendingPaymentQueryRequest? query,
        CancellationToken cancellationToken)
    {
        var actor = _actor.TryResolveListActor();
        if (actor is Guid userId)
        {
            return await _store.ListOwnedByUserAsync(userId, 50, cancellationToken);
        }

        var proofs = (query?.Proofs ?? [])
            .Where(x => x.CheckoutId != Guid.Empty && x.CartId != Guid.Empty)
            .Take(20)
            .ToArray();
        if (proofs.Length == 0)
        {
            return [];
        }

        var ids = proofs.Select(x => x.CheckoutId).Distinct().ToArray();
        var groups = await _store.ListByCheckoutIdsAsync(ids, cancellationToken);
        var allowed = new List<CheckoutGroup>();
        foreach (var group in groups)
        {
            var proof = proofs.FirstOrDefault(x => x.CheckoutId == group.CheckoutId);
            if (proof is null || proof.CartId != group.CartId)
            {
                continue;
            }

            var owned = await _carts.TryGetForOwnershipAsync(group.CartId, proof.GuestSecret, cancellationToken);
            if (owned is null)
            {
                continue;
            }

            allowed.Add(group);
        }

        return allowed;
    }

    private async Task<HashSet<Guid>> LoadHiddenCheckoutIdsAsync(
        IReadOnlyList<CheckoutGroup> groups,
        CancellationToken cancellationToken)
    {
        if (groups.Count == 0)
        {
            return [];
        }

        var checkoutIds = groups.Select(x => x.CheckoutId).ToArray();
        var actor = _actor.TryResolveListActor();
        if (actor is Guid userId)
        {
            return await _store.LoadHiddenCheckoutIdsAsync(checkoutIds, userId, Array.Empty<Guid>(), cancellationToken);
        }

        var cartIds = groups.Select(x => x.CartId).Distinct().ToArray();
        return await _store.LoadHiddenCheckoutIdsAsync(checkoutIds, null, cartIds, cancellationToken);
    }

    private async Task<Dictionary<Guid, (string Title, Guid? MediaAssetId)>> LoadLineCatalogAsync(
        IReadOnlyList<CheckoutGroup> groups,
        CancellationToken cancellationToken)
    {
        var variantIds = groups
            .SelectMany(x => x.SellerOrders.SelectMany(o => o.Lines.Select(l => l.CatalogVariantId)))
            .Distinct()
            .ToArray();
        var result = new Dictionary<Guid, (string Title, Guid? MediaAssetId)>();
        if (variantIds.Length == 0)
        {
            return result;
        }

        var presentations = await _catalog.GetVariantPresentationsAsync(variantIds, cancellationToken);
        foreach (var variantId in variantIds)
        {
            if (presentations.TryGetValue(variantId, out var row))
            {
                result[variantId] = (
                    string.IsNullOrWhiteSpace(row.LocalizedTitle) ? "کالا" : row.LocalizedTitle,
                    row.MediaAssetId);
            }
            else
            {
                result[variantId] = ("کالا", null);
            }
        }

        return result;
    }
}
