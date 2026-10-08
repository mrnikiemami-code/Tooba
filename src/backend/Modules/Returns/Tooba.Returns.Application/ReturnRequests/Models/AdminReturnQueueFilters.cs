using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Application.ReturnRequests.Models;

/// <summary>
/// Admin return/refund work-queue projection policy: the fast queue filters and the status/action
/// projection over the real Return domain status. Kept in its own cohesive file (split out of the
/// former mixed <c>AdminReturnWorkQueueModels</c> file) because it is policy, not a read model.
/// </summary>
public static class AdminReturnQueueFilters
{
    /// <summary>All.</summary>
    public const string All = "all";
    /// <summary>Awaiting review.</summary>
    public const string PendingReview = "pending_review";
    /// <summary>Approved.</summary>
    public const string Approved = "approved";
    /// <summary>Received and awaiting refund.</summary>
    public const string ReceivedAwaitingRefund = "refund_needed";
    /// <summary>Refund pending.</summary>
    public const string RefundPending = "refund_pending";
    /// <summary>Refund failed.</summary>
    public const string RefundFailed = "refund_failed";
    /// <summary>Completed.</summary>
    public const string Completed = "completed";
    /// <summary>Rejected.</summary>
    public const string Rejected = "rejected";

    /// <summary>Known fast-filter codes.</summary>
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

    /// <summary>Whether the domain status matches the fast queue filter.</summary>
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

    /// <summary>Return status, kept separate from the refund status.</summary>
    public static string ComposeReturnStatus(ReturnRequestStatus status) => status switch
    {
        ReturnRequestStatus.Requested => "Requested",
        ReturnRequestStatus.Rejected => "Rejected",
        ReturnRequestStatus.Cancelled => "Cancelled",
        ReturnRequestStatus.Completed => "Completed",
        _ => "Approved",
    };

    /// <summary>Refund status: none / pending / failed / completed.</summary>
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

    /// <summary>Kebab action codes for the domain status.</summary>
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
    /// Eligibility summary from the line snapshot plus the last delivery; the current Offer policy is
    /// never recomputed here. This is a display-label composition (like the certified Fulfillment /
    /// Catalog / Promotion work-queue label fallbacks), not an error message and not part of the stable
    /// error-code / localization surface: the snapshot policy label itself is customer/merchant-authored
    /// data that is passed through unchanged.
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
