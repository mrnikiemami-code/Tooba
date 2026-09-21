using Tooba.Inventory.Application.Ports;
using Tooba.Inventory.Application.Checkout;
using Tooba.Inventory.Application.Orders;
using Tooba.Inventory.Application.Returns;
using Tooba.Order.Application;
using Tooba.Order.Domain;

namespace Tooba.Host.Admin;

/// <summary>برچسب‌های انسانی ممیزی چرخه رزرو؛ enum خام به UI نمی‌رود.</summary>
public static class AdminReservationCycleMapper
{
    /// <summary>وضعیت کسب‌وکاری فارسی.</summary>
    public static string StatusFa(ReservationCycleStatus? status) => status switch
    {
        ReservationCycleStatus.Active => "رزرو فعال",
        ReservationCycleStatus.Expired => "مهلت رزرو پایان یافته",
        ReservationCycleStatus.ReleasedByCancel => "رزرو با لغو سفارش آزاد شد",
        ReservationCycleStatus.ReleasedByPolicy => "رزرو با سیاست آزاد شد",
        ReservationCycleStatus.ReacquireFailed => "رزرو مجدد ناموفق",
        ReservationCycleStatus.CommittedPaid => "رزرو پس از پرداخت نهایی شد",
        _ => "بدون چرخه رزرو",
    };

    /// <summary>وضعیت کسب‌وکاری انگلیسی.</summary>
    public static string StatusEn(ReservationCycleStatus? status) => status switch
    {
        ReservationCycleStatus.Active => "Active reservation",
        ReservationCycleStatus.Expired => "Reservation hold expired",
        ReservationCycleStatus.ReleasedByCancel => "Reservation released by cancel",
        ReservationCycleStatus.ReleasedByPolicy => "Reservation released by policy",
        ReservationCycleStatus.ReacquireFailed => "Reacquire failed",
        ReservationCycleStatus.CommittedPaid => "Reservation committed after payment",
        _ => "No reservation cycle",
    };

    /// <summary>دلیل شروع چرخه فارسی.</summary>
    public static string ReasonFa(ReservationCycleReason? reason) => reason switch
    {
        ReservationCycleReason.InitialPayment => "رزرو اولیه پرداخت",
        ReservationCycleReason.RetryAfterExpiry => "رزرو مجدد پس از پایان مهلت",
        ReservationCycleReason.ManualInitial => "در انتظار ثبت اطلاعات پرداخت",
        ReservationCycleReason.ManualReview => "در انتظار بررسی پرداخت",
        ReservationCycleReason.Restore => "بازیابی سفارش",
        ReservationCycleReason.HistoricalRecovery => "بازیابی تاریخی",
        ReservationCycleReason.LatePaymentRecovery => "بازیابی پرداخت دیرهنگام",
        _ => "—",
    };

    /// <summary>دلیل شروع چرخه انگلیسی.</summary>
    public static string ReasonEn(ReservationCycleReason? reason) => reason switch
    {
        ReservationCycleReason.InitialPayment => "Initial payment reservation",
        ReservationCycleReason.RetryAfterExpiry => "Retry after hold expiry",
        ReservationCycleReason.ManualInitial => "Awaiting payment details",
        ReservationCycleReason.ManualReview => "Awaiting payment review",
        ReservationCycleReason.Restore => "Order restore",
        ReservationCycleReason.HistoricalRecovery => "Historical recovery",
        ReservationCycleReason.LatePaymentRecovery => "Late payment recovery",
        _ => "—",
    };

    /// <summary>برچسب فشرده گرید فارسی.</summary>
    public static string CompactFa(ReservationCycleStatus? status, int? cycleNumber) => status switch
    {
        ReservationCycleStatus.Active => cycleNumber is int n ? $"فعال #{n}" : "فعال",
        ReservationCycleStatus.Expired => cycleNumber is int n ? $"پایان‌یافته #{n}" : "پایان‌یافته",
        ReservationCycleStatus.CommittedPaid => "نهایی‌شده",
        ReservationCycleStatus.ReleasedByCancel or ReservationCycleStatus.ReleasedByPolicy =>
            cycleNumber is int n ? $"آزادشده #{n}" : "آزادشده",
        ReservationCycleStatus.ReacquireFailed => "رزرو مجدد ناموفق",
        _ => "—",
    };

    /// <summary>برچسب فشرده گرید انگلیسی.</summary>
    public static string CompactEn(ReservationCycleStatus? status, int? cycleNumber) => status switch
    {
        ReservationCycleStatus.Active => cycleNumber is int n ? $"Active #{n}" : "Active",
        ReservationCycleStatus.Expired => cycleNumber is int n ? $"Expired #{n}" : "Expired",
        ReservationCycleStatus.CommittedPaid => "Committed",
        ReservationCycleStatus.ReleasedByCancel or ReservationCycleStatus.ReleasedByPolicy =>
            cycleNumber is int n ? $"Released #{n}" : "Released",
        ReservationCycleStatus.ReacquireFailed => "Reacquire failed",
        _ => "—",
    };

    /// <summary>کلید فیلتر پایدار.</summary>
    public static string StateKey(ReservationCycleStatus? status) => status switch
    {
        ReservationCycleStatus.Active => "active",
        ReservationCycleStatus.Expired => "expired",
        ReservationCycleStatus.CommittedPaid => "committed",
        ReservationCycleStatus.ReleasedByCancel or ReservationCycleStatus.ReleasedByPolicy => "released",
        ReservationCycleStatus.ReacquireFailed => "reacquireFailed",
        _ => "none",
    };

    /// <summary>منبع سیاست ذخیره‌شده؛ Settings جاری بازنویسی نمی‌کند.</summary>
    public static string PolicySourceFa(string? source) => (source ?? "").Trim().ToLowerInvariant() switch
    {
        "offer" => "پیشنهاد",
        "category" => "دسته",
        "store" => "فروشگاه",
        "platform" => "پلتفرم",
        _ => string.IsNullOrWhiteSpace(source) ? "پلتفرم" : source.Trim(),
    };

    /// <summary>منبع سیاست انگلیسی.</summary>
    public static string PolicySourceEn(string? source) => (source ?? "").Trim().ToLowerInvariant() switch
    {
        "offer" => "Offer",
        "category" => "Category",
        "store" => "Store",
        "platform" => "Platform",
        _ => string.IsNullOrWhiteSpace(source) ? "Platform" : source.Trim(),
    };

    /// <summary>وضعیت نمایشی با رویدادهای ممیزی (شکست بازتملک روی چرخهٔ منقضی).</summary>
    public static ReservationCycleStatus? EffectiveStatus(
        ReservationCycleProjection projection,
        IReadOnlyList<ReservationCycleEventSnapshot>? events)
    {
        if (projection.CurrentStatus is ReservationCycleStatus.Active or ReservationCycleStatus.CommittedPaid)
        {
            return projection.CurrentStatus;
        }

        if (events is { Count: > 0 })
        {
            var last = events[^1];
            if (last.Kind == ReservationCycleEventKind.ReacquireFailed)
            {
                return ReservationCycleStatus.ReacquireFailed;
            }

            if (last.Kind == ReservationCycleEventKind.RetryLimitReached
                && events.Any(e => e.Kind == ReservationCycleEventKind.ReacquireFailed))
            {
                return ReservationCycleStatus.ReacquireFailed;
            }
        }

        return projection.CurrentStatus;
    }

    /// <summary>خلاصهٔ فشرده برای گرید سفارش/پرداخت.</summary>
    public static AdminReservationCycleSummary ToSummary(ReservationCycleProjection? projection)
    {
        if (projection is null || (projection.CurrentStatus is null && projection.TotalCyclesCreated == 0))
        {
            return EmptySummary();
        }

        var status = projection.CurrentStatus;
        var retryLimit = projection.RetryCountRemaining <= 0
            && status is not ReservationCycleStatus.Active
            && status is not ReservationCycleStatus.CommittedPaid
            && projection.TotalCyclesCreated > 0;
        var retryPossible = projection.RetryCountRemaining > 0
            && status is ReservationCycleStatus.Expired or ReservationCycleStatus.ReleasedByPolicy;
        var needsReacquire = status is not ReservationCycleStatus.Active
            && status is not ReservationCycleStatus.CommittedPaid
            && projection.SupplyStatus is "AvailableForReacquire" or "Unavailable" or "PartiallyUnavailable";

        return new AdminReservationCycleSummary(
            CompactFa(status, projection.CurrentCycleNumber),
            CompactEn(status, projection.CurrentCycleNumber),
            StateKey(status),
            projection.CurrentCycleNumber,
            retryPossible,
            needsReacquire,
            retryLimit);
    }

    /// <summary>ممیزی کامل جزئیات سفارش از تاریخچهٔ immutable.</summary>
    public static AdminReservationCycleAuditView ToAudit(
        ReservationCycleProjection projection,
        IReadOnlyList<ReservationCycleEventSnapshot> events,
        OrderSupplyStatus? supply)
    {
        var status = EffectiveStatus(projection, events);
        var current = projection.History.LastOrDefault(x => x.Status == ReservationCycleStatus.Active)
            ?? projection.History.LastOrDefault();
        var retryLimit = projection.RetryCountRemaining <= 0
            && status is not ReservationCycleStatus.Active
            && status is not ReservationCycleStatus.CommittedPaid
            && projection.TotalCyclesCreated > 0;
        var shortages = status == ReservationCycleStatus.ReacquireFailed
            || events.Any(e => e.Kind == ReservationCycleEventKind.ReacquireFailed)
            ? ShortageLines(supply)
            : [];

        return new AdminReservationCycleAuditView(
            StatusFa(status),
            StatusEn(status),
            ReasonFa(current?.Reason),
            ReasonEn(current?.Reason),
            projection.CurrentCycleNumber,
            projection.TotalCyclesCreated,
            projection.EffectiveMaxCycles,
            projection.RetryCountRemaining,
            projection.StartedAt,
            projection.ExpiresAt,
            projection.ServerTime,
            projection.SecondsRemaining,
            projection.SupplyStatus ?? supply?.Status.ToString() ?? "NotApplicable",
            OrderSupplyComposer.MessageFa(supply?.Status ?? ParseSupply(projection.SupplyStatus)),
            retryLimit,
            CanRetryReservation: false,
            CanExtendTimer: false,
            projection.History.Select(ToHistoryRow).ToArray(),
            events.Select(e => ToEventView(e, projection.History)).ToArray(),
            shortages);
    }

    /// <summary>خلاصهٔ خالی گرید.</summary>
    public static AdminReservationCycleSummary EmptySummary() =>
        new("—", "—", "none", null, false, false, false);

    private static AdminReservationCycleHistoryRow ToHistoryRow(ReservationCycleSnapshot cycle) =>
        new(
            cycle.CycleNumber,
            StatusFa(cycle.Status),
            StatusEn(cycle.Status),
            ReasonFa(cycle.Reason),
            ReasonEn(cycle.Reason),
            cycle.StartedAt,
            cycle.ExpiresAt,
            cycle.EndedAt,
            cycle.EffectiveHoldMinutes,
            cycle.EffectiveMaxCycles,
            cycle.PolicySource,
            PolicySourceFa(cycle.PolicySource),
            PolicySourceEn(cycle.PolicySource),
            HumanPaymentRef(cycle.PaymentAttemptId));

    private static AdminReservationCycleEventView ToEventView(
        ReservationCycleEventSnapshot ev,
        IReadOnlyList<ReservationCycleSnapshot> history)
    {
        var cycleNumber = ev.CycleId is Guid id
            ? history.FirstOrDefault(x => x.CycleId == id)?.CycleNumber
            : null;
        var (fa, en, detailFa, detailEn) = EventCopy(ev.Kind);
        return new AdminReservationCycleEventView(
            fa,
            en,
            ev.OccurredAt,
            cycleNumber,
            detailFa,
            detailEn);
    }

    private static (string Fa, string En, string DetailFa, string DetailEn) EventCopy(ReservationCycleEventKind kind) =>
        kind switch
        {
            ReservationCycleEventKind.Started =>
                ("شروع چرخه", "Cycle started", "چرخه رزرو آغاز شد.", "Reservation cycle started."),
            ReservationCycleEventKind.Expired =>
                ("پایان مهلت", "Hold expired", "مهلت رزرو این چرخه پایان یافت.", "This cycle hold expired."),
            ReservationCycleEventKind.ReleasedByCancel =>
                ("آزادسازی با لغو", "Released by cancel", "رزرو با لغو سفارش آزاد شد.", "Reservation released by cancel."),
            ReservationCycleEventKind.ReacquireRequested =>
                ("درخواست رزرو مجدد", "Reacquire requested", "بازگشت موجودی درخواست شد.", "Stock reacquire was requested."),
            ReservationCycleEventKind.ReacquireFailed =>
                ("شکست رزرو مجدد", "Reacquire failed", "رزرو مجدد به دلیل کمبود موجودی انجام نشد.", "Reacquire failed because stock was insufficient."),
            ReservationCycleEventKind.StartedAfterRetry =>
                ("شروع پس از تلاش مجدد", "Started after retry", "چرخه جدید پس از بازتملک موفق آغاز شد.", "A new cycle started after successful reacquire."),
            ReservationCycleEventKind.CommittedPaid =>
                ("نهایی‌سازی پس از پرداخت", "Committed after payment", "رزرو پس از پرداخت موفق نهایی شد.", "Reservation committed after successful payment."),
            ReservationCycleEventKind.RetryLimitReached =>
                ("سقف دفعات رزرو", "Retry limit reached", "حداکثر دفعات رزرو این سفارش استفاده شده است.", "Maximum reservation cycles for this order have been used."),
            ReservationCycleEventKind.ManualReviewTransitioned =>
                ("گذار به بررسی دستی", "Manual review", "سفارش وارد بررسی پرداخت شد.", "The order entered payment review."),
            ReservationCycleEventKind.ReleasedByPolicy =>
                ("آزادسازی سیاستی", "Released by policy", "رزرو با سیاست آزاد شد.", "Reservation released by policy."),
            _ => ("رویداد رزرو", "Reservation event", "—", "—"),
        };

    private static IReadOnlyList<AdminReservationShortageLine> ShortageLines(OrderSupplyStatus? supply)
    {
        if (supply is null)
        {
            return [];
        }

        return supply.Lines
            .Where(x => x.Shortage > 0
                || x.LineStatus is OrderSupplyStatusKind.Unavailable or OrderSupplyStatusKind.PartiallyUnavailable)
            .Select(x => new AdminReservationShortageLine(
                string.IsNullOrWhiteSpace(x.ItemTitle) ? "قلم" : x.ItemTitle,
                x.Required,
                x.Available,
                x.Shortage,
                x.UnitCode))
            .ToArray();
    }

    private static string? HumanPaymentRef(Guid? paymentAttemptId)
    {
        if (paymentAttemptId is not { } id || id == Guid.Empty)
        {
            return null;
        }

        return id.ToString("N")[^8..];
    }

    private static OrderSupplyStatusKind ParseSupply(string? value) =>
        Enum.TryParse<OrderSupplyStatusKind>(value, true, out var kind) ? kind : OrderSupplyStatusKind.NotApplicable;
}
