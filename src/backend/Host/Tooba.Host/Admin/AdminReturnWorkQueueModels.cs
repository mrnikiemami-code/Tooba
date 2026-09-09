using Tooba.Returns.Domain;

namespace Tooba.Host.Admin;

/// <summary>
/// ردیف صف کار مرجوعی/بازگشت وجه Admin — همان دامنه Return، بدون lifecycle دوم.
/// </summary>
public sealed record AdminReturnWorkQueueRow(
    Guid ReturnRequestId,
    Guid SellerOrderId,
    Guid CheckoutId,
    Guid SellerPartyId,
    string ReturnReference,
    string OrderReference,
    string CustomerDisplayName,
    string SellerDisplayName,
    string ProductLabel,
    decimal QuantityRequested,
    string UnitLabel,
    string ReturnStatus,
    string RefundStatus,
    string EligibilitySummary,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<string> AvailableActionCodes);

/// <summary>فیلترهای سریع صف مرجوعی بر اساس وضعیت واقعی دامنه.</summary>
public static class AdminReturnQueueFilters
{
    /// <summary>همه.</summary>
    public const string All = "all";
    /// <summary>در انتظار بررسی.</summary>
    public const string PendingReview = "pending_review";
    /// <summary>تأییدشده.</summary>
    public const string Approved = "approved";
    /// <summary>نیازمند بازگشت وجه.</summary>
    public const string ReceivedAwaitingRefund = "refund_needed";
    /// <summary>بازگشت وجه در انتظار.</summary>
    public const string RefundPending = "refund_pending";
    /// <summary>بازگشت وجه ناموفق.</summary>
    public const string RefundFailed = "refund_failed";
    /// <summary>تکمیل‌شده.</summary>
    public const string Completed = "completed";
    /// <summary>ردشده.</summary>
    public const string Rejected = "rejected";

    /// <summary>کدهای شناخته‌شدهٔ فیلتر سریع.</summary>
    public static readonly HashSet<string> Known = new(StringComparer.OrdinalIgnoreCase)
    {
        All,
        PendingReview,
        Approved,
        ReceivedAwaitingRefund,
        RefundPending,
        RefundFailed,
        Completed,
        Rejected,
    };

    /// <summary>آیا وضعیت دامنه با فیلتر سریع صف مطابقت دارد.</summary>
    public static bool Matches(ReturnRequestStatus status, string queueFilter)
    {
        return queueFilter.Trim().ToLowerInvariant() switch
        {
            PendingReview => status == ReturnRequestStatus.Requested,
            Approved => status is ReturnRequestStatus.Approved or ReturnRequestStatus.RefundProcessing,
            ReceivedAwaitingRefund => status == ReturnRequestStatus.Approved,
            RefundPending => status == ReturnRequestStatus.RefundProcessing,
            RefundFailed => status == ReturnRequestStatus.RefundFailed,
            Completed => status == ReturnRequestStatus.Completed,
            Rejected => status == ReturnRequestStatus.Rejected,
            _ => true,
        };
    }

    /// <summary>وضعیت مرجوعی جدا از وضعیت بازگشت وجه.</summary>
    public static string ComposeReturnStatus(ReturnRequestStatus status) => status switch
    {
        ReturnRequestStatus.Requested => "Requested",
        ReturnRequestStatus.Rejected => "Rejected",
        ReturnRequestStatus.Cancelled => "Cancelled",
        ReturnRequestStatus.Completed => "Completed",
        _ => "Approved",
    };

    /// <summary>وضعیت بازگشت وجه: none / pending / failed / completed.</summary>
    public static string ComposeRefundStatus(ReturnRequestStatus status, IReadOnlyList<RefundAttemptStatus> attempts)
    {
        if (status is ReturnRequestStatus.Requested or ReturnRequestStatus.Rejected or ReturnRequestStatus.Cancelled)
        {
            return "none";
        }

        if (status == ReturnRequestStatus.Completed
            || attempts.Any(x => x == RefundAttemptStatus.Succeeded))
        {
            return "completed";
        }

        if (status == ReturnRequestStatus.RefundFailed
            || (attempts.Any(x => x == RefundAttemptStatus.Failed)
                && attempts.All(x => x != RefundAttemptStatus.Succeeded)))
        {
            return "failed";
        }

        if (status is ReturnRequestStatus.RefundProcessing or ReturnRequestStatus.Approved
            || attempts.Any(x => x == RefundAttemptStatus.Pending))
        {
            return "pending";
        }

        return "none";
    }

    /// <summary>کدهای عملیاتی kebab برای وضعیت دامنه.</summary>
    public static IReadOnlyList<string> ProjectActionCodes(ReturnRequestStatus status)
    {
        return status switch
        {
            ReturnRequestStatus.Requested => ["approve_return", "reject_return"],
            ReturnRequestStatus.RefundFailed => ["retry_refund"],
            _ => [],
        };
    }

    /// <summary>
    /// خلاصهٔ eligibility از snapshot خط + آخرین تحویل؛ سیاست جاری Offer دوباره محاسبه نمی‌شود.
    /// </summary>
    public static string ComposeEligibilitySummary(
        bool isReturnable,
        int windowDays,
        string? policyLabel,
        DateTimeOffset? lastDeliveredAt,
        DateTimeOffset now)
    {
        if (!isReturnable)
        {
            return string.IsNullOrWhiteSpace(policyLabel) ? "غیرقابل مرجوعی" : policyLabel.Trim();
        }

        var days = windowDays > 0 ? windowDays : 7;
        var relative = string.IsNullOrWhiteSpace(policyLabel)
            ? $"{days} روز پس از تحویل"
            : policyLabel.Trim();
        if (lastDeliveredAt is null)
        {
            return relative;
        }

        var deadline = lastDeliveredAt.Value.AddDays(days);
        if (now > deadline)
        {
            return "منقضی";
        }

        var remaining = deadline - now;
        var remainingText = remaining.TotalDays >= 1
            ? $"{Math.Ceiling(remaining.TotalDays):0} روز باقی‌مانده"
            : $"{Math.Max(1, (int)Math.Ceiling(remaining.TotalHours))} ساعت باقی‌مانده";
        return $"{remainingText} · مهلت {deadline:yyyy-MM-dd}";
    }
}
