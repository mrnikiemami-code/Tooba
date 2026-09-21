using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks.Grid;
using Tooba.Host.Admin;
using Tooba.Order.Application;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Payment.Application.Ports;
using Tooba.Host.Storefront;

namespace Tooba.Host.Grid;

/// <summary>پرس‌وجوی Admin پرداخت‌ها از طریق IPaymentQueryDirectory + enrich Order (بدون دسترسی مستقیم به schema پرداخت در Host).</summary>
internal sealed class AdminPaymentsGridQueryEngine
{
    private readonly IPaymentQueryDirectory _payments;
    private readonly OrderDbContext _orders;
    private readonly OrderSupplyComposer _supply;
    private readonly IReservationCycleDirectory _cycles;

    public AdminPaymentsGridQueryEngine(
        IPaymentQueryDirectory payments,
        OrderDbContext orders,
        OrderSupplyComposer supply,
        IReservationCycleDirectory cycles)
    {
        _payments = payments;
        _orders = orders;
        _supply = supply;
        _cycles = cycles;
    }

    public async Task<GridPageResponse<AdminReceiptListItem>> QueryAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Guid>? restrict = null;
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            restrict = await _orders.Checkouts.AsNoTracking()
                .Where(c =>
                    c.RecipientName.ToLower().Contains(term)
                    || c.CheckoutId.ToString().ToLower().Contains(term)
                    || c.SellerOrders.Any(o => o.OrderNumber.ToLower().Contains(term)))
                .Select(c => c.CheckoutId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        foreach (var filter in request.Filters.Where(f => f.Field is "supply" or "reservation"))
        {
            var checkoutIds = await ResolveCheckoutFilterAsync(filter, cancellationToken).ConfigureAwait(false);
            restrict = restrict is null ? checkoutIds : restrict.Intersect(checkoutIds).ToList();
        }

        var sort = request.Sort.FirstOrDefault() ?? new GridSortRequest("created", "desc");
        var page = await _payments.QueryAdminGridAsync(
            new PaymentAdminGridQueryDto(
                request.Search,
                request.Filters
                    .Where(f => f.Field is not ("supply" or "reservation"))
                    .Select(f => new PaymentAdminGridFilterDto(f.Field, f.Operator, f.Value, f.ValueTo, f.Values))
                    .ToList(),
                sort.Field,
                sort.Direction,
                request.Page,
                request.PageSize,
                restrict),
            cancellationToken).ConfigureAwait(false);

        var items = await MapPageAsync(page.Items, cancellationToken).ConfigureAwait(false);
        return new GridPageResponse<AdminReceiptListItem>(items, request.Page, request.PageSize, page.Total);
    }

    private async Task<IReadOnlyList<Guid>> ResolveCheckoutFilterAsync(
        GridFilterRequest filter,
        CancellationToken cancellationToken)
    {
        var wanted = (filter.Values ?? [])
            .Concat(string.IsNullOrWhiteSpace(filter.Value) ? [] : [filter.Value!])
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (wanted.Count == 0)
        {
            return [];
        }

        var checkoutIds = await _orders.Checkouts.AsNoTracking()
            .Select(x => x.CheckoutId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (filter.Field == "supply")
        {
            var statuses = await _supply.GetStatusesAsync(checkoutIds, cancellationToken).ConfigureAwait(false);
            return checkoutIds.Where(id =>
                    statuses.TryGetValue(id, out var st) && wanted.Contains(st.Status.ToString()))
                .ToList();
        }

        var supply = await _supply.GetStatusesAsync(checkoutIds, cancellationToken).ConfigureAwait(false);
        var supplyByCheckout = supply.ToDictionary(x => x.Key, x => (string?)x.Value.Status.ToString());
        var cycles = await _cycles.GetProjectionsAsync(
            checkoutIds,
            DateTimeOffset.UtcNow,
            supplyByCheckout,
            cancellationToken).ConfigureAwait(false);
        return checkoutIds.Where(id =>
        {
            var summary = cycles.TryGetValue(id, out var projection)
                ? AdminReservationCycleMapper.ToSummary(projection)
                : AdminReservationCycleMapper.EmptySummary();
            return wanted.Contains(summary.State) || wanted.Contains(summary.CompactLabelFa);
        }).ToList();
    }

    private async Task<IReadOnlyList<AdminReceiptListItem>> MapPageAsync(
        IReadOnlyList<PaymentCheckoutRowDto> rows,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return [];
        }

        var checkoutIds = rows.Select(x => x.CheckoutId).Distinct().ToList();
        var checkouts = await _orders.Checkouts.AsNoTracking()
            .Include(x => x.SellerOrders)
            .Where(x => checkoutIds.Contains(x.CheckoutId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var checkoutMap = checkouts.ToDictionary(x => x.CheckoutId);
        var supply = await _supply.GetStatusesAsync(checkoutIds, cancellationToken).ConfigureAwait(false);
        var supplyByCheckout = supply.ToDictionary(x => x.Key, x => (string?)x.Value.Status.ToString());
        var cycles = await _cycles.GetProjectionsAsync(
            checkoutIds,
            DateTimeOffset.UtcNow,
            supplyByCheckout,
            cancellationToken).ConfigureAwait(false);

        return rows.Select(payment =>
        {
            checkoutMap.TryGetValue(payment.CheckoutId, out var checkout);
            var references = checkout?.SellerOrders
                .Select(x => x.OrderNumber)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList() ?? [];
            var reference = references.Count == 0
                ? payment.CheckoutId.ToString("N")[..12]
                : string.Join(" / ", references);
            var customer = checkout is null
                ? "مشتری توبا"
                : StorefrontRecipientNames.DisplayOrFallback(checkout.RecipientFirstName, checkout.RecipientLastName, checkout.RecipientName);
            var supplyStatus = checkout is null
                ? "NotApplicable"
                : supply.TryGetValue(payment.CheckoutId, out var st) ? st.Status.ToString() : "NotApplicable";
            var reservation = cycles.TryGetValue(payment.CheckoutId, out var projection)
                ? AdminReservationCycleMapper.ToSummary(projection)
                : AdminReservationCycleMapper.EmptySummary();
            return new AdminReceiptListItem(
                payment.PaymentId,
                payment.CheckoutId,
                reference,
                customer,
                payment.Amount,
                payment.Currency,
                payment.Status,
                payment.ProviderCode,
                payment.CreatedAt,
                payment.CompletedAt,
                supplyStatus,
                reservation.CompactLabelFa,
                reservation.CompactLabelEn,
                reservation.State,
                reservation.CycleNumber,
                reservation.RetryPossible,
                reservation.NeedsReacquire,
                reservation.RetryLimitReached);
        }).ToList();
    }
}
