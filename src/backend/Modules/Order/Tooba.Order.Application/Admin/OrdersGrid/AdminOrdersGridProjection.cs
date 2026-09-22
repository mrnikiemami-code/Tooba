using Tooba.Order.Application.Admin.OrdersGrid.Models;
using Tooba.Order.Domain;
using Tooba.Returns.Contracts.Operations;

namespace Tooba.Order.Application.Admin.OrdersGrid;

/// <summary>نگاشت ردیف گرید سفارش‌های مدیر؛ خالص و مستقل از persistence.</summary>
public static class AdminOrdersGridProjection
{
    /// <summary>نگاشت ردیف فهرست سفارش با وضعیت عملیاتی ترکیب‌شده.</summary>
    public static AdminOrderListItem MapOrderListItem(
        CheckoutGroup group,
        IReadOnlyDictionary<Guid, string> sellerNames,
        IReadOnlyDictionary<Guid, IReadOnlyList<ReturnSnapshot>> returnsBySellerOrder,
        string supplyStatus = "NotApplicable",
        OrderReservationCycleSummary? reservation = null)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(sellerNames);
        ArgumentNullException.ThrowIfNull(returnsBySellerOrder);

        var orders = group.SellerOrders;
        reservation ??= OrderReservationCycleSummaryMapper.EmptySummary();
        var references = orders.Select(x => x.OrderNumber).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        var statuses = orders.Select(x => x.Status).Distinct().ToList();
        var relatedReturns = orders
            .SelectMany(o => returnsBySellerOrder.TryGetValue(o.SellerOrderId, out var list) ? list : [])
            .Select(r => r.Status)
            .ToList();
        var composedStatus = ComposeOperationalStatus(statuses, relatedReturns);
        return new AdminOrderListItem(
            group.CheckoutId,
            references.Count == 0 ? group.CheckoutId.ToString("N")[..12] : string.Join(" / ", references),
            group.SubmittedAt,
            RecipientDisplay(group.RecipientFirstName, group.RecipientLastName, group.RecipientName),
            orders.Count,
            FormatSellerDisplayNames(orders, sellerNames),
            InvoiceHeaderSemantics.LineCount(orders),
            orders.Sum(x => x.GrandTotalSnapshot),
            orders.Select(x => x.Currency).FirstOrDefault() ?? "IRR",
            orders.Count > 0 && orders.All(x => x.Status == SellerOrderStatus.Cancelled)
                ? "Cancelled"
                : orders.Count > 0 && orders.All(x => x.Status == SellerOrderStatus.Paid)
                    ? "Paid"
                    : "PendingPayment",
            composedStatus,
            string.IsNullOrWhiteSpace(supplyStatus) ? "NotApplicable" : supplyStatus,
            reservation.CompactLabelFa,
            reservation.CompactLabelEn,
            reservation.State,
            reservation.CycleNumber,
            reservation.RetryPossible,
            reservation.NeedsReacquire,
            reservation.RetryLimitReached);
    }

    /// <summary>
    /// وضعیت عملیاتی فهرست: در صورت مرجوعی/بازگشت وجه، سلول وضعیت را از آن می‌سازد.
    /// </summary>
    public static string ComposeOperationalStatus(
        IReadOnlyList<SellerOrderStatus> orderStatuses,
        IReadOnlyList<ReturnRequestOperationStatus> returnStatuses)
    {
        ArgumentNullException.ThrowIfNull(orderStatuses);
        ArgumentNullException.ThrowIfNull(returnStatuses);

        if (returnStatuses.Count > 0)
        {
            if (returnStatuses.Any(s => s == ReturnRequestOperationStatus.RefundFailed))
            {
                return "RefundFailed";
            }

            if (returnStatuses.Any(s => s == ReturnRequestOperationStatus.RefundProcessing))
            {
                return "RefundPending";
            }

            if (returnStatuses.Any(s => s == ReturnRequestOperationStatus.Requested))
            {
                return "ReturnRequested";
            }

            if (returnStatuses.Any(s => s == ReturnRequestOperationStatus.Approved))
            {
                return "ReturnApproved";
            }

            if (returnStatuses.Any(s => s == ReturnRequestOperationStatus.Completed)
                && returnStatuses.All(s => s is ReturnRequestOperationStatus.Completed
                    or ReturnRequestOperationStatus.Rejected
                    or ReturnRequestOperationStatus.Cancelled))
            {
                return "RefundCompleted";
            }
        }

        return orderStatuses.Count == 1 ? orderStatuses[0].ToString() : "Mixed";
    }

    private static string FormatSellerDisplayNames(
        IEnumerable<SellerOrder> orders,
        IReadOnlyDictionary<Guid, string> sellerNames)
    {
        var sellerIds = orders.Select(o => o.SellerPartyId).Distinct().ToList();
        if (sellerIds.Count == 0)
        {
            return "—";
        }

        if (sellerIds.Count == 1)
        {
            return sellerNames.TryGetValue(sellerIds[0], out var name) && !string.IsNullOrWhiteSpace(name)
                ? name
                : "—";
        }

        return $"{sellerIds.Count} فروشنده";
    }

    private static string RecipientDisplay(string? firstName, string? lastName, string? recipientName)
    {
        var first = firstName?.Trim() ?? string.Empty;
        var last = lastName?.Trim() ?? string.Empty;
        var display = first.Length > 0 && last.Length > 0
            ? $"{first} {last}"
            : recipientName?.Trim() ?? string.Empty;
        return display.Length == 0 ? "مشتری توبا" : display;
    }
}
