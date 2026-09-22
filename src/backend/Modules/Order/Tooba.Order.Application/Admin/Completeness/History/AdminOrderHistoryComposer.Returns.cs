using Tooba.Order.Domain;
using Tooba.Returns.Contracts.History;
using Tooba.Settlement.Contracts.History;

namespace Tooba.Order.Application.Admin.Completeness.History;

public static partial class AdminOrderHistoryComposer
{
    private static readonly string[] ApprovedReturnStatuses =
    [
        ReturnHistoryStatuses.Approved,
        ReturnHistoryStatuses.RefundProcessing,
        ReturnHistoryStatuses.Completed,
        ReturnHistoryStatuses.RefundFailed,
    ];

    private static void AppendReturns(
        List<AdminOrderHistoryDraft> entries,
        CheckoutGroup group,
        AdminOrderHistoryScope scope,
        IReadOnlyList<ReturnHistoryRecord> returns)
    {
        var ordersBySellerOrderId = group.SellerOrders.ToDictionary(x => x.SellerOrderId);
        foreach (var record in returns)
        {
            var sellerLabel = ordersBySellerOrderId.TryGetValue(record.SellerOrderId, out var order)
                ? scope.SellerName(order.SellerPartyId)
                : AdminOrderHistoryFormatting.FallbackSellerName;
            var returnScope = scope.FormatLineQtyScope(
                record.Items.Select(x => (x.OrderLineId, x.Quantity)).ToList());
            var summary = string.IsNullOrWhiteSpace(returnScope) ? sellerLabel : returnScope;

            entries.Add(Draft(
                record.CreatedAt,
                "return_requested",
                "مرجوعی",
                "Return requested",
                record.RequestedByUserId == Guid.Empty ? null : record.RequestedByUserId,
                summary,
                summary));

            if (ApprovedReturnStatuses.Contains(record.Status))
            {
                entries.Add(Draft(
                    record.UpdatedAt,
                    "return_approved",
                    "تأیید مرجوعی",
                    "Return approved",
                    null,
                    sellerLabel,
                    sellerLabel));
            }

            if (record.Status == ReturnHistoryStatuses.Rejected)
            {
                entries.Add(Draft(
                    record.UpdatedAt,
                    "return_rejected",
                    "رد مرجوعی",
                    "Return rejected",
                    null,
                    sellerLabel,
                    sellerLabel));
            }

            AppendRefundAttempts(entries, record);
        }
    }

    private static void AppendRefundAttempts(List<AdminOrderHistoryDraft> entries, ReturnHistoryRecord record)
    {
        foreach (var attempt in record.RefundAttempts)
        {
            var occurredAt = attempt.CompletedAt ?? attempt.CreatedAt;
            switch (attempt.Status)
            {
                case ReturnHistoryRefundAttemptStatuses.Succeeded:
                    entries.Add(Draft(
                        occurredAt,
                        "refund_completed",
                        "بازگشت وجه",
                        "Refund completed",
                        null,
                        $"{AdminOrderHistoryFormatting.FormatMoneyFa(attempt.Amount)} ریال",
                        $"{attempt.Amount:0} {attempt.Currency}"));
                    break;
                case ReturnHistoryRefundAttemptStatuses.Failed:
                    entries.Add(Draft(
                        occurredAt,
                        "refund_failed",
                        "شکست بازگشت وجه",
                        "Refund failed",
                        null));
                    break;
                case ReturnHistoryRefundAttemptStatuses.Pending
                    when record.Status == ReturnHistoryStatuses.RefundFailed:
                    entries.Add(Draft(
                        attempt.CreatedAt,
                        "refund_retried",
                        "تلاش مجدد بازگشت وجه",
                        "Refund retried",
                        null));
                    break;
            }
        }
    }

    private static void AppendSettlements(
        List<AdminOrderHistoryDraft> entries,
        CheckoutGroup group,
        IReadOnlyList<SettlementHistoryEntry> settlementEntries)
    {
        foreach (var entry in settlementEntries)
        {
            var orderNumber = group.SellerOrders
                    .FirstOrDefault(x => x.SellerOrderId == entry.SellerOrderId)?.OrderNumber
                ?? entry.SellerOrderId.ToString("N")[..8];

            var (kind, labelFa, labelEn) = ClassifySettlement(entry);
            entries.Add(Draft(
                entry.PostedAt,
                kind,
                labelFa,
                labelEn,
                null,
                $"سفارش {orderNumber}",
                $"Order {orderNumber}"));
        }
    }

    private static (string Kind, string LabelFa, string LabelEn) ClassifySettlement(SettlementHistoryEntry entry)
    {
        if (string.Equals(entry.SourceType, SettlementHistorySourceTypes.OrderCancel, StringComparison.OrdinalIgnoreCase))
        {
            return ("settlement_cancel_adjustment", "تعدیل سهم فروشنده ثبت شد", "Seller cancel adjustment");
        }

        if (string.Equals(entry.SourceType, SettlementHistorySourceTypes.Refund, StringComparison.OrdinalIgnoreCase)
            || string.Equals(entry.EntryType, SettlementHistoryEntryTypes.Debit, StringComparison.OrdinalIgnoreCase))
        {
            return ("settlement_adjustment", "تعدیل تسویه (مرجوعی)", "Seller refund adjustment");
        }

        return ("settlement_accrual", "ثبت تسویه فروشنده", "Seller settlement accrual");
    }
}
