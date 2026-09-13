namespace Tooba.Order.Domain;

/// <summary>وضعیت چرخهٔ رزرو سفارش؛ با PaymentAttempt یکی نیست.</summary>
public enum ReservationCycleStatus
{
    /// <summary>چرخهٔ جاری با مهلت معتبر.</summary>
    Active = 0,

    /// <summary>مهلت سرور گذشته و چرخه بسته شده است.</summary>
    Expired = 1,

    /// <summary>لغو سفارش رزرو را آزاد کرد.</summary>
    ReleasedByCancel = 2,

    /// <summary>آزادسازی سیاستی (رد مدرک / انقضای بازبینی).</summary>
    ReleasedByPolicy = 3,

    /// <summary>تلاش بازتملک شکست خورد؛ شمارهٔ چرخه مصرف نشده است.</summary>
    ReacquireFailed = 4,

    /// <summary>پرداخت موفق؛ رزرو بادوام.</summary>
    CommittedPaid = 5,
}

/// <summary>دلیل شروع چرخه؛ از وضعیت جدا است.</summary>
public enum ReservationCycleReason
{
    /// <summary>شروع پرداخت آنلاین روی سفارش متعهد.</summary>
    InitialPayment = 0,

    /// <summary>بازتملک پس از پایان چرخهٔ قبلی.</summary>
    RetryAfterExpiry = 1,

    /// <summary>شروع کارت‌به‌کارت / دستی.</summary>
    ManualInitial = 2,

    /// <summary>فاز بازبینی مدرک داخل همان چرخه یا چرخهٔ بازبینی.</summary>
    ManualReview = 3,

    /// <summary>بازگردانی سفارش لغو شده.</summary>
    Restore = 4,

    /// <summary>بازیابی تاریخی بدون زنده کردن ردیف Released.</summary>
    HistoricalRecovery = 5,

    /// <summary>وصول دیرهنگام پس از انقضا.</summary>
    LatePaymentRecovery = 6,
}

/// <summary>رویداد ممیزی چرخه؛ بازنویسی نمی‌شود.</summary>
public enum ReservationCycleEventKind
{
    /// <summary>شروع چرخه.</summary>
    Started = 0,

    /// <summary>انقضای چرخه.</summary>
    Expired = 1,

    /// <summary>آزادسازی با لغو.</summary>
    ReleasedByCancel = 2,

    /// <summary>درخواست بازتملک.</summary>
    ReacquireRequested = 3,

    /// <summary>شکست بازتملک.</summary>
    ReacquireFailed = 4,

    /// <summary>شروع چرخه پس از retry.</summary>
    StartedAfterRetry = 5,

    /// <summary>تعهد پرداخت موفق.</summary>
    CommittedPaid = 6,

    /// <summary>سقف تعداد چرخه.</summary>
    RetryLimitReached = 7,

    /// <summary>گذار به بازبینی دستی داخل همان چرخه.</summary>
    ManualReviewTransitioned = 8,

    /// <summary>آزادسازی سیاستی.</summary>
    ReleasedByPolicy = 9,
}

/// <summary>چرخهٔ رزرو سطح سفارش؛ ردیف موجودی را زنده نمی‌کند.</summary>
public sealed class ReservationCycle
{
    /// <summary>سازندهٔ EF.</summary>
    private ReservationCycle()
    {
    }

    /// <summary>شناسهٔ پایدار چرخه.</summary>
    public Guid CycleId { get; init; }

    /// <summary>سفارش مالک.</summary>
    public Guid CheckoutId { get; init; }

    /// <summary>شمارهٔ یکنواخت در سفارش؛ هرگز بازنویسی نمی‌شود.</summary>
    public int CycleNumber { get; init; }

    /// <summary>دلیل شروع.</summary>
    public ReservationCycleReason Reason { get; private set; }

    /// <summary>وضعیت کسب‌وکاری.</summary>
    public ReservationCycleStatus Status { get; private set; }

    /// <summary>شروع سروری.</summary>
    public DateTimeOffset StartedAt { get; init; }

    /// <summary>مهلت سروری؛ کلاینت تمدید نمی‌کند.</summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>پایان چرخه.</summary>
    public DateTimeOffset? EndedAt { get; private set; }

    /// <summary>بازیگر یا همبستگی.</summary>
    public string? Actor { get; init; }

    /// <summary>کلید همبستگی/idempotency.</summary>
    public string? CorrelationId { get; init; }

    /// <summary>تلاش پرداخت اختیاری؛ هویت چرخه نیست.</summary>
    public Guid? PaymentAttemptId { get; private set; }

    /// <summary>دقایق مهلت مؤثر در لحظهٔ شروع.</summary>
    public int EffectiveHoldMinutes { get; init; }

    /// <summary>سقف چرخه در لحظهٔ شروع.</summary>
    public int EffectiveMaxCycles { get; init; }

    /// <summary>منبع تقدم سیاست.</summary>
    public string PolicySource { get; init; } = "platform";

    /// <summary>شناسه‌های رزرو همین چرخه به‌صورت CSV.</summary>
    public string ReservationIds { get; private set; } = string.Empty;

    /// <summary>چرخهٔ اولیه یا retry را می‌سازد.</summary>
    public static ReservationCycle Start(
        Guid checkoutId,
        int cycleNumber,
        ReservationCycleReason reason,
        DateTimeOffset startedAt,
        DateTimeOffset expiresAt,
        int effectiveHoldMinutes,
        int effectiveMaxCycles,
        string policySource,
        IReadOnlyList<Guid> reservationIds,
        string? actor,
        string? correlationId,
        Guid? paymentAttemptId)
    {
        if (cycleNumber < 1)
        {
            throw new InvalidOperationException("شمارهٔ چرخه رزرو باید از ۱ شروع شود.");
        }

        if (expiresAt <= startedAt)
        {
            throw new InvalidOperationException("مهلت چرخه رزرو باید پس از شروع باشد.");
        }

        return new ReservationCycle
        {
            CycleId = Guid.NewGuid(),
            CheckoutId = checkoutId,
            CycleNumber = cycleNumber,
            Reason = reason,
            Status = ReservationCycleStatus.Active,
            StartedAt = startedAt,
            ExpiresAt = expiresAt,
            Actor = actor,
            CorrelationId = correlationId,
            PaymentAttemptId = paymentAttemptId,
            EffectiveHoldMinutes = effectiveHoldMinutes,
            EffectiveMaxCycles = effectiveMaxCycles,
            PolicySource = string.IsNullOrWhiteSpace(policySource) ? "platform" : policySource.Trim(),
            ReservationIds = string.Join(',', reservationIds.Distinct()),
        };
    }

    /// <summary>تلاش پرداخت داخل چرخه فعال مهلت را عوض نمی‌کند.</summary>
    public void CorrelatePaymentAttempt(Guid? paymentAttemptId)
    {
        EnsureActive();
        if (paymentAttemptId is { } id)
        {
            PaymentAttemptId = id;
        }
    }

    /// <summary>گذار بازبینی دستی: همان شماره، مهلت جدید، بدون بازنویسی تاریخچه.</summary>
    public void TransitionManualReview(DateTimeOffset reviewExpiresAt, DateTimeOffset now)
    {
        EnsureActive();
        if (reviewExpiresAt <= now)
        {
            throw new InvalidOperationException("مهلت بازبینی باید در آینده باشد.");
        }

        Reason = ReservationCycleReason.ManualReview;
        ExpiresAt = reviewExpiresAt;
    }

    /// <summary>چرخهٔ فعال را با وضعیت پایانی می‌بندد.</summary>
    public void Close(ReservationCycleStatus status, DateTimeOffset endedAt)
    {
        EnsureActive();
        if (status is ReservationCycleStatus.Active or ReservationCycleStatus.ReacquireFailed)
        {
            throw new InvalidOperationException("وضعیت پایانی چرخه نامعتبر است.");
        }

        Status = status;
        EndedAt = endedAt;
    }

    /// <summary>رزروهای بازتملک‌شده را به چرخهٔ فعال پیوند می‌دهد؛ ردیف قدیمی را زنده نمی‌کند.</summary>
    public void RebindReservations(IReadOnlyList<Guid> reservationIds)
    {
        EnsureActive();
        ReservationIds = string.Join(',', reservationIds.Distinct());
    }

    /// <summary>شناسه‌های رزرو همین چرخه.</summary>
    public IReadOnlyList<Guid> ParseReservationIds()
    {
        if (string.IsNullOrWhiteSpace(ReservationIds))
        {
            return [];
        }

        return ReservationIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Guid.Parse)
            .ToArray();
    }

    private void EnsureActive()
    {
        if (Status != ReservationCycleStatus.Active)
        {
            throw new InvalidOperationException("چرخهٔ بسته‌شده بازنویسی نمی‌شود.");
        }
    }
}

/// <summary>رویداد ممیزی append-only برای چرخه رزرو.</summary>
public sealed class ReservationCycleEvent
{
    /// <summary>سازندهٔ EF.</summary>
    private ReservationCycleEvent()
    {
    }

    /// <summary>شناسه رویداد.</summary>
    public Guid EventId { get; init; }

    /// <summary>سفارش.</summary>
    public Guid CheckoutId { get; init; }

    /// <summary>چرخه اختیاری؛ شکست بازتملک ممکن است بدون شماره باشد.</summary>
    public Guid? CycleId { get; init; }

    /// <summary>نوع رویداد.</summary>
    public ReservationCycleEventKind Kind { get; init; }

    /// <summary>زمان سرور.</summary>
    public DateTimeOffset OccurredAt { get; init; }

    /// <summary>جزئیات کوتاه ممیزی.</summary>
    public string Detail { get; init; } = string.Empty;

    /// <summary>رویداد جدید.</summary>
    public static ReservationCycleEvent Record(
        Guid checkoutId,
        Guid? cycleId,
        ReservationCycleEventKind kind,
        DateTimeOffset occurredAt,
        string detail) =>
        new()
        {
            EventId = Guid.NewGuid(),
            CheckoutId = checkoutId,
            CycleId = cycleId,
            Kind = kind,
            OccurredAt = occurredAt,
            Detail = detail.Length > 256 ? detail[..256] : detail,
        };
}
