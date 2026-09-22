using Tooba.Order.Domain;
using Tooba.Payment.Contracts.Admin;

namespace Tooba.Order.Application.Admin.Completeness.History;

public static partial class AdminOrderHistoryComposer
{
    private const string ManualDepositRejectedFailureCode = "MANUAL_DEPOSIT_REJECTED";

    private static void AppendPayment(
        List<AdminOrderHistoryDraft> entries,
        CheckoutGroup group,
        PaymentAdminOperationalSnapshot? payment)
    {
        if (payment is null)
        {
            return;
        }

        var money = $"{payment.Amount:0} {payment.Currency}";
        var updatedAt = payment.UpdatedAt == default ? payment.CreatedAt : payment.UpdatedAt;
        entries.Add(Draft(
            payment.CreatedAt,
            "payment_created",
            "ایجاد پرداخت",
            "Payment created",
            null,
            money,
            money));

        switch (payment.Status)
        {
            case "Pending":
                entries.Add(Draft(updatedAt, "payment_pending", "پرداخت در انتظار", "Payment pending", null));
                if (payment.HasManualDepositRejection)
                {
                    entries.Add(Draft(
                        updatedAt,
                        "payment_deposit_restored",
                        "بازگرداندن به انتظار تأیید واریز",
                        "Deposit restored to pending confirmation",
                        null));
                }

                break;
            case "Succeeded":
                entries.Add(Draft(
                    payment.CompletedAt ?? payment.UpdatedAt,
                    "payment_succeeded",
                    "پرداخت موفق",
                    "Payment succeeded",
                    null,
                    money,
                    money));
                break;
            case "Failed":
                var rejected = payment.HasManualDepositRejection
                    || payment.LastFailureCode == ManualDepositRejectedFailureCode;
                entries.Add(Draft(
                    payment.UpdatedAt,
                    rejected ? "payment_deposit_rejected" : "payment_failed",
                    rejected ? "رد واریز" : "پرداخت ناموفق",
                    rejected ? "Deposit rejected" : "Payment failed",
                    null,
                    payment.LastFailureCode,
                    payment.LastFailureCode));
                break;
            case "Cancelled":
                entries.Add(Draft(payment.UpdatedAt, "payment_cancelled", "لغو پرداخت", "Payment cancelled", null));
                break;
            case "Expired":
                entries.Add(Draft(payment.UpdatedAt, "payment_expired", "انقضای پرداخت", "Payment expired", null));
                break;
            case "RefundPending":
                entries.Add(Draft(
                    payment.UpdatedAt,
                    "payment_refund_pending",
                    group.SellerOrders.Any(x => x.Status == SellerOrderStatus.Cancelled)
                        ? "بازگشت وجه آغاز شد"
                        : "بازگشت وجه در انتظار",
                    "Refund pending",
                    null));
                break;
            case "Refunded":
                entries.Add(Draft(
                    payment.UpdatedAt,
                    "payment_refunded",
                    "بازگشت وجه",
                    "Refunded",
                    null,
                    money,
                    money));
                break;
            case "RefundFailed":
                entries.Add(Draft(
                    payment.UpdatedAt,
                    "payment_refund_failed",
                    "شکست بازگشت وجه",
                    "Refund failed",
                    null,
                    payment.LastFailureCode,
                    payment.LastFailureCode));
                break;
        }
    }
}
