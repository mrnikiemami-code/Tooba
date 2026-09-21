using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Fulfillment.Application.Ports;
using Tooba.Fulfillment.Application.Models;
using Tooba.Fulfillment.Application.Shipping;
using Tooba.Host.Admin;
using Tooba.Order.Application;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Payment.Application.Ports;
using Tooba.Payment.Domain.ValueObjects;
using Tooba.Settlement.Application;

namespace Tooba.Host.Storefront;

/// <summary>
/// تصویر دسته‌ای در انتظار پرداخت. مالکیت مهمان فقط اثبات متعهد است؛ سبد فعال منبع اختیار نیست.
/// </summary>
public sealed class StorefrontPendingPaymentComposer
{
    private const string DevActorHeader = "X-Tooba-Dev-Actor-User-Id";

    private readonly OrderDbContext _orders;
    private readonly IPaymentQueryDirectory _paymentQueries;
    private readonly CatalogDbContext _catalog;
    private readonly StorefrontCartComposer _carts;
    private readonly IReservationCycleDirectory _cycles;
    private readonly ICheckoutDirectory _checkout;
    private readonly IFulfillmentDirectory _fulfillment;
    private readonly IPaymentAdminDirectory _paymentOps;
    private readonly ISettlementDirectory _settlement;
    private readonly CurrentAuthenticatedSession _session;
    private readonly IHostEnvironment _environment;
    private readonly IHttpContextAccessor _http;

    /// <summary>ترکیب Host برای فهرست در انتظار پرداخت.</summary>
    internal StorefrontPendingPaymentComposer(
        OrderDbContext orders,
        IPaymentQueryDirectory paymentQueries,
        CatalogDbContext catalog,
        StorefrontCartComposer carts,
        IReservationCycleDirectory cycles,
        ICheckoutDirectory checkout,
        IFulfillmentDirectory fulfillment,
        IPaymentAdminDirectory paymentOps,
        ISettlementDirectory settlement,
        CurrentAuthenticatedSession session,
        IHostEnvironment environment,
        IHttpContextAccessor http)
    {
        _orders = orders;
        _paymentQueries = paymentQueries;
        _catalog = catalog;
        _carts = carts;
        _cycles = cycles;
        _checkout = checkout;
        _fulfillment = fulfillment;
        _paymentOps = paymentOps;
        _settlement = settlement;
        _session = session;
        _environment = environment;
        _http = http;
    }

    /// <summary>فهرست سفارش‌های unpaid قابل‌اقدام یا اطلاع‌رسانی را برمی‌گرداند.</summary>
    public async Task<StorefrontPendingPaymentPage> ListAsync(
        StorefrontPendingPaymentQueryRequest? query,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var groups = await ResolveOwnedGroupsAsync(query, cancellationToken);
        if (groups.Count == 0)
        {
            return new StorefrontPendingPaymentPage(now, []);
        }

        var checkoutIds = groups.Select(x => x.CheckoutId).ToArray();
        var paymentRows = await _paymentQueries.GetLatestByCheckoutIdsAsync(checkoutIds, cancellationToken);
        var latestPayments = paymentRows.ToDictionary(
            row => row.CheckoutId,
            row => new StorefrontPendingPaymentProjector.PaymentInput(
                row.PaymentId,
                Enum.TryParse<PaymentStatus>(row.Status, true, out var st) ? st : PaymentStatus.Pending,
                row.ProviderCode,
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
        var now = DateTimeOffset.UtcNow;
        var cycle = (await _cycles.GetProjectionsAsync([group.CheckoutId], now, null, cancellationToken))
            .GetValueOrDefault(group.CheckoutId);
        if (cycle is { CurrentStatus: ReservationCycleStatus.Active, SecondsRemaining: > 0 })
        {
            throw new InvalidOperationException(
                "pending.hide.active_hold: تا پایان مهلت رزرو نمی‌توان کارت را پنهان کرد.");
        }

        var actor = ResolveListActor();
        var ownerUserId = actor;
        var guestCartId = actor is null ? group.CartId : (Guid?)null;
        var exists = await _orders.PendingPaymentCardHides.AsNoTracking()
            .AnyAsync(
                x => x.CheckoutId == group.CheckoutId
                    && (ownerUserId != null
                        ? x.OwnerUserId == ownerUserId
                        : x.GuestCartId == guestCartId),
                cancellationToken);
        if (!exists)
        {
            _orders.PendingPaymentCardHides.Add(
                PendingPaymentCardHide.Create(group.CheckoutId, ownerUserId, guestCartId, now));
            await _orders.SaveChangesAsync(cancellationToken);
        }

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
            throw new InvalidOperationException("سفارش پیدا نشد.");
        }

        if (AdminOrderOperationsComposer.IsCheckoutCancelled(group))
        {
            return new { ok = true, checkoutId = group.CheckoutId, alreadyCancelled = true };
        }

        if (group.SellerOrders.Any(x => x.Status == SellerOrderStatus.Paid))
        {
            throw new InvalidOperationException(
                "order.cancel.unpaid_only: لغو این سفارش از سبد فقط قبل از پرداخت موفق امکان‌پذیر است.");
        }

        var payment = await _paymentOps.GetLatestOperationalForCheckoutAsync(group.CheckoutId, cancellationToken);
        if (payment is not null
            && payment.Status is PaymentStatus.Succeeded
                or PaymentStatus.RefundPending
                or PaymentStatus.Refunded
                or PaymentStatus.RefundFailed)
        {
            throw new InvalidOperationException(
                "order.cancel.unpaid_only: لغو این سفارش از سبد فقط قبل از پرداخت موفق امکان‌پذیر است.");
        }

        var fulfillments = await _fulfillment.ListForCheckoutAsync(group.CheckoutId, cancellationToken);
        if (AdminOrderOperationsComposer.HasDispatchedOrDelivered(fulfillments))
        {
            throw new InvalidOperationException(
                $"order.cancel.forbidden: {AdminOrderOperationsComposer.WholeOrderCancelBlockedAfterDispatchFa}");
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
            throw new InvalidOperationException("order.cancel.forbidden: لغو در وضعیت فعلی سفارش مجاز نیست.");
        }

        if (payment is not null)
        {
            await _settlement.NeutralizeUnpaidAccrualForCancelAsync(
                payment.PaymentId,
                group.SellerOrders.Select(x => x.SellerOrderId).ToList(),
                cancellationToken);
        }

        await _paymentOps.CloseOrStartRefundForOrderCancelAsync(group.CheckoutId, cancellationToken);
        return new { ok = true, checkoutId = group.CheckoutId, alreadyCancelled = false };
    }

    private async Task<CheckoutGroup> ResolveOwnedGroupForCancelAsync(
        Guid checkoutId,
        Guid cartId,
        string? guestSecret,
        CancellationToken cancellationToken)
    {
        var group = await _orders.Checkouts.AsNoTracking()
            .Include(x => x.SellerOrders)
            .SingleOrDefaultAsync(x => x.CheckoutId == checkoutId, cancellationToken)
            ?? throw new InvalidOperationException("سفارش پیدا نشد.");

        var actor = ResolveListActor();
        if (actor is Guid userId)
        {
            if (group.PlacedByUserId != userId)
            {
                throw new InvalidOperationException("checkout.access.denied");
            }

            if (cartId != Guid.Empty && cartId != group.CartId)
            {
                throw new InvalidOperationException("checkout.access.denied");
            }

            return group;
        }

        if (cartId == Guid.Empty || cartId != group.CartId)
        {
            throw new InvalidOperationException("checkout.access.denied");
        }

        var owned = await _carts.TryGetForOwnershipAsync(group.CartId, guestSecret, cancellationToken);
        if (owned is null)
        {
            throw new InvalidOperationException("checkout.access.denied");
        }

        return group;
    }

    private async Task<IReadOnlyList<CheckoutGroup>> ResolveOwnedGroupsAsync(
        StorefrontPendingPaymentQueryRequest? query,
        CancellationToken cancellationToken)
    {
        var actor = ResolveListActor();
        if (actor is Guid userId)
        {
            return await _orders.Checkouts.AsNoTracking()
                .Include(x => x.SellerOrders)
                .ThenInclude(x => x.Lines)
                .Where(x => x.PlacedByUserId == userId)
                .OrderByDescending(x => x.SubmittedAt)
                .Take(50)
                .ToListAsync(cancellationToken);
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
        var groups = await _orders.Checkouts.AsNoTracking()
            .Include(x => x.SellerOrders)
            .ThenInclude(x => x.Lines)
            .Where(x => ids.Contains(x.CheckoutId))
            .ToListAsync(cancellationToken);
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
        var actor = ResolveListActor();
        if (actor is Guid userId)
        {
            var rows = await _orders.PendingPaymentCardHides.AsNoTracking()
                .Where(x => x.OwnerUserId == userId && checkoutIds.Contains(x.CheckoutId))
                .Select(x => x.CheckoutId)
                .ToListAsync(cancellationToken);
            return [.. rows];
        }

        var cartIds = groups.Select(x => x.CartId).Distinct().ToArray();
        var guestRows = await _orders.PendingPaymentCardHides.AsNoTracking()
            .Where(x => x.GuestCartId != null && cartIds.Contains(x.GuestCartId.Value) && checkoutIds.Contains(x.CheckoutId))
            .Select(x => x.CheckoutId)
            .ToListAsync(cancellationToken);
        return [.. guestRows];
    }

    private Guid? ResolveListActor()
    {
        if (_session.IsAuthenticated && _session.UserId is Guid userId && userId != Guid.Empty)
        {
            return userId;
        }

        var request = _http.HttpContext?.Request;
        var isDevSeam = _environment.IsDevelopment() || _environment.IsEnvironment("Testing");
        if (isDevSeam
            && request is not null
            && request.Headers.TryGetValue(DevActorHeader, out var raw)
            && Guid.TryParse(raw.ToString(), out var headerActor)
            && headerActor != Guid.Empty)
        {
            return headerActor;
        }

        return null;
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

        var variants = await _catalog.Variants.AsNoTracking()
            .Where(x => variantIds.Contains(x.VariantId))
            .Select(x => new { x.VariantId, x.ProductId })
            .ToListAsync(cancellationToken);
        var productIds = variants.Select(x => x.ProductId).Distinct().ToArray();
        var names = productIds.Length == 0
            ? []
            : await _catalog.LocalizedTexts.AsNoTracking()
                .Where(x =>
                    x.OwnerKind == CatalogLocalizedOwnerKind.Product
                    && productIds.Contains(x.OwnerId)
                    && x.FieldKey == "name")
                .ToListAsync(cancellationToken);
        var productName = names
            .GroupBy(x => x.OwnerId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(row => row.Locale.StartsWith("fa", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                    .First().Value);
        var media = productIds.Length == 0
            ? []
            : await _catalog.MediaReferences.AsNoTracking()
                .Where(x => productIds.Contains(x.ProductId))
                .Select(x => new { x.ProductId, x.MediaAssetId })
                .ToListAsync(cancellationToken);
        var productMedia = media
            .GroupBy(x => x.ProductId)
            .ToDictionary(g => g.Key, g => (Guid?)g.First().MediaAssetId);
        var variantProduct = variants.ToDictionary(x => x.VariantId, x => x.ProductId);
        foreach (var variantId in variantIds)
        {
            variantProduct.TryGetValue(variantId, out var productId);
            productName.TryGetValue(productId, out var title);
            productMedia.TryGetValue(productId, out var asset);
            result[variantId] = (string.IsNullOrWhiteSpace(title) ? "کالا" : title, asset);
        }

        return result;
    }
}
