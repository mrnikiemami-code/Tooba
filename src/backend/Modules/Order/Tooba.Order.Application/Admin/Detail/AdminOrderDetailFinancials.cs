using Tooba.Order.Application.Admin.Detail.Models;
using Tooba.Order.Application.Storefront.Services;
using Tooba.Order.Domain;
using Tooba.Settlement.Contracts.Operations;

namespace Tooba.Order.Application.Admin.Detail;

/// <summary>Projection سابقه مالی جزئیات سفارش مدیر.</summary>
public static class AdminOrderDetailFinancials
{
    /// <summary>ورودی محدود برای projection بازگشت وجه سفارش.</summary>
    public sealed record OrderFinancialRefundInput(
        Guid ReturnRequestId,
        Guid RefundAttemptId,
        decimal Amount,
        string Currency,
        DateTimeOffset OccurredAt,
        decimal ExpectedRefundAmount,
        string? Reference = null);

    /// <summary>
    /// حرکات مالی واقعی قابل‌انتساب به همین سفارش (بدون مبلغ کل batch payout).
    /// </summary>
    public static IReadOnlyList<AdminFinancialEventView> BuildFinancialEvents(
        CheckoutGroup group,
        IReadOnlyDictionary<Guid, string> sellerNames,
        AdminPaymentOpsView? payment,
        IReadOnlyDictionary<Guid, IReadOnlyList<SettlementAdminOrderEntrySnapshot>> settlementByOrder,
        IReadOnlyList<OrderFinancialRefundInput> succeededRefunds)
    {
        var events = new List<AdminFinancialEventView>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        void AddOnce(string key, AdminFinancialEventView row)
        {
            if (!seen.Add(key))
            {
                return;
            }

            events.Add(row);
        }

        if (payment is not null && IsSuccessfulPaymentStatus(payment.Status))
        {
            AddOnce(
                $"receipt:{payment.PaymentId:N}",
                new AdminFinancialEventView(
                    payment.CompletedAt ?? payment.CreatedAt,
                    "CustomerReceipt",
                    payment.Amount,
                    payment.Currency,
                    StorefrontRecipientNames.DisplayOrFallback(group.RecipientFirstName, group.RecipientLastName, group.RecipientName),
                    payment.ProviderTransactionReference
                        ?? payment.ProviderRequestReference
                        ?? payment.PaymentId.ToString("N")[..12],
                    HumanizeProviderCode(payment.ProviderCode),
                    "Succeeded",
                    "دریافت از مشتری"));
        }

        foreach (var refund in succeededRefunds
                     .GroupBy(x => x.ReturnRequestId)
                     .Select(g => g.OrderByDescending(x => x.OccurredAt).First()))
        {
            var expected = refund.ExpectedRefundAmount > 0 ? refund.ExpectedRefundAmount : refund.Amount;
            var description = refund.Amount < expected
                ? "بازگشت وجه جزئی به مشتری"
                : "بازگشت وجه به مشتری";
            AddOnce(
                $"refund:{refund.ReturnRequestId:N}",
                new AdminFinancialEventView(
                    refund.OccurredAt,
                    "CustomerRefund",
                    refund.Amount,
                    refund.Currency,
                    StorefrontRecipientNames.DisplayOrFallback(group.RecipientFirstName, group.RecipientLastName, group.RecipientName),
                    string.IsNullOrWhiteSpace(refund.Reference)
                        ? refund.RefundAttemptId.ToString("N")[..12]
                        : refund.Reference!,
                    "بازگشت وجه",
                    "Succeeded",
                    description));
        }

        foreach (var order in group.SellerOrders)
        {
            if (!settlementByOrder.TryGetValue(order.SellerOrderId, out var entries))
            {
                continue;
            }

            sellerNames.TryGetValue(order.SellerPartyId, out var sellerName);
            foreach (var entry in entries)
            {
                var isCancelNeutralize = string.Equals(entry.SourceType, "order_cancel", StringComparison.OrdinalIgnoreCase);
                var isRefundAdj = !isCancelNeutralize
                    && (string.Equals(entry.SourceType, "refund", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(entry.EntryType, "Debit", StringComparison.OrdinalIgnoreCase));
                AddOnce(
                    $"settlement:{entry.EntryId:N}",
                    new AdminFinancialEventView(
                        entry.PostedAt,
                        isCancelNeutralize
                            ? "SellerCancelAdjustment"
                            : isRefundAdj ? "SellerRefundAdjustment" : "SellerPayout",
                        entry.NetAmount,
                        entry.Currency,
                        sellerName ?? "فروشنده",
                        entry.EntryId.ToString("N")[..12],
                        isCancelNeutralize ? "خنثی‌سازی لغو" : isRefundAdj ? "تعدیل مرجوعی" : "تسویه سفارش",
                        "Succeeded",
                        isCancelNeutralize
                            ? "خنثی‌سازی بدهی فروشنده بابت لغو سفارش"
                            : isRefundAdj
                                ? "کسر از حساب فروشنده بابت بازگشت وجه"
                                : "واریز سهم فروشنده"));
            }
        }

        return events
            .OrderByDescending(x => x.OccurredAt)
            .ThenBy(x => x.EventType, StringComparer.Ordinal)
            .ThenBy(x => x.Reference, StringComparer.Ordinal)
            .ToList();
    }

    public static IReadOnlyList<AdminSellerFinancialView> BuildSellerFinancials(
        CheckoutGroup group,
        IReadOnlyDictionary<Guid, string> sellerNames,
        IReadOnlyDictionary<Guid, IReadOnlyList<SettlementAdminOrderEntrySnapshot>> settlementByOrder)
    {
        return group.SellerOrders.Select(order =>
        {
            sellerNames.TryGetValue(order.SellerPartyId, out var sellerName);
            settlementByOrder.TryGetValue(order.SellerOrderId, out var entries);
            var credit = entries?.FirstOrDefault(x =>
                string.Equals(x.EntryType, "Credit", StringComparison.OrdinalIgnoreCase));
            decimal gross;
            decimal commission;
            decimal payable;
            string settlementStatus;
            if (credit is not null)
            {
                gross = credit.GrossAmount;
                commission = credit.CommissionAmount;
                payable = credit.NetAmount;
                settlementStatus = "Settled";
            }
            else
            {
                gross = order.SubtotalSnapshot;
                commission = 0m;
                payable = order.GrandTotalSnapshot;
                settlementStatus = order.Status == SellerOrderStatus.Paid
                    ? "WaitingForSettlement"
                    : "NotSettled";
            }

            return new AdminSellerFinancialView(
                order.SellerOrderId,
                order.SellerPartyId,
                sellerName ?? "فروشنده",
                InvoiceHeaderSemantics.LineCount(order),
                gross,
                commission,
                payable,
                order.Currency,
                settlementStatus);
        }).ToList();
    }

    public static AdminFinancialSummaryView BuildFinancialSummary(
        CheckoutGroup group,
        IReadOnlyList<AdminSellerFinancialView> sellerFinancials,
        AdminPaymentOpsView? payment)
    {
        var currency = group.SellerOrders.Select(x => x.Currency).FirstOrDefault() ?? "IRR";
        var totalSellerShare = sellerFinancials.Sum(x => x.GrossAmount);
        var totalCommission = sellerFinancials.Sum(x => x.CommissionAmount);
        var payableToSellers = sellerFinancials.Sum(x => x.PayableAmount);
        var customerGross = group.SellerOrders.Sum(x => x.SubtotalSnapshot);
        var shippingCost = 0m;
        var customerDiscounts = group.SellerOrders.Sum(x => x.DiscountSnapshot);
        var totalReceived = payment?.Amount ?? group.SellerOrders.Sum(x => x.GrandTotalSnapshot);
        return new AdminFinancialSummaryView(
            totalSellerShare,
            totalCommission,
            totalCommission,
            payableToSellers,
            customerGross,
            shippingCost,
            customerDiscounts,
            totalReceived,
            currency);
    }

    public static string HumanizeProviderCode(string? providerCode) =>
        providerCode?.Trim().ToLowerInvariant() switch
        {
            "wallet" => "کیف پول",
            "fake" => "درگاه آزمایشی",
            "manual" => "کارت به کارت",
            "webhook" => "درگاه وب‌هوک",
            "fail-closed" => "درگاه غیرفعال",
            null or "" => "—",
            _ => providerCode!
        };

    public static bool IsSuccessfulPaymentStatus(string status) =>
        string.Equals(status, "Succeeded", StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, "Captured", StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, "Paid", StringComparison.OrdinalIgnoreCase);
}
