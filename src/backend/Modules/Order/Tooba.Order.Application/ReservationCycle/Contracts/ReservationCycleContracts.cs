using Tooba.Order.Domain;


namespace Tooba.Order.Application.ReservationCycle.Contracts;

/// <summary>کلیدهای سیاست چرخه رزرو از Settings.</summary>
public sealed class ReservationCycleOptions
{
    /// <summary>بخش پیکربندی.</summary>
    public const string SectionName = "ReservationCycle";

    /// <summary>مهلت چرخهٔ اولیه (دقیقه) در سطح platform.</summary>
    public int InitialReservationHoldMinutes { get; set; } = 120;

    /// <summary>مهلت چرخهٔ retry (دقیقه) در سطح platform.</summary>
    public int RetryReservationHoldMinutes { get; set; } = 120;

    /// <summary>سقف تعداد چرخه شامل اولیه.</summary>
    public int MaxReservationCycles { get; set; } = 3;
}

/// <summary>خط لازم برای حل سیاست چندقلم.</summary>
public sealed record ReservationCyclePolicyLine(Guid OfferId, Guid? CategoryId);

/// <summary>سیاست مؤثر سفارش در لحظهٔ شروع چرخه.</summary>
public sealed record ReservationCyclePolicySnapshot(
    int InitialHoldMinutes,
    int RetryHoldMinutes,
    int MaxCycles,
    string Source);

/// <summary>یک لایهٔ حل‌شده با منبع جدا برای هر فیلد.</summary>
public sealed record ReservationPolicyLayerPreview(
    int InitialHoldMinutes,
    string InitialSource,
    int RetryHoldMinutes,
    string RetrySource,
    int MaxCycles,
    string MaxSource);

/// <summary>پیش‌نمایش Admin: override هر سطح + مؤثر backend-resolved.</summary>
public sealed record ReservationPolicyPreview(
    ReservationPolicyLayerPreview Platform,
    ReservationPolicyLayerPreview AfterStore,
    ReservationPolicyLayerPreview AfterCategory,
    ReservationPolicyLayerPreview AfterOffer,
    int? StoreInitialOverride,
    int? StoreRetryOverride,
    int? StoreMaxOverride,
    int? CategoryInitialOverride,
    int? CategoryRetryOverride,
    int? CategoryMaxOverride,
    int? OfferInitialOverride,
    int? OfferRetryOverride,
    int? OfferMaxOverride);

/// <summary>حل Offer &gt; Category &gt; Store &gt; Platform و سپس حداقل چندخط.</summary>
public interface IReservationCyclePolicyResolver
{
    /// <summary>سیاست مؤثر خطوط لازم را برمی‌گرداند.</summary>
    Task<ReservationCyclePolicySnapshot> ResolveAsync(
        IReadOnlyList<ReservationCyclePolicyLine> lines,
        CancellationToken cancellationToken);

    /// <summary>پیش‌نمایش فیلدبه‌فیلد برای Admin؛ تقدم را فرانت محاسبه نمی‌کند.</summary>
    Task<ReservationPolicyPreview> PreviewAsync(
        Guid? offerId,
        Guid? categoryId,
        CancellationToken cancellationToken);

    /// <summary>پیش‌نمایش دسته‌ای بدون N+1 خواندن store/overrides.</summary>
    Task<IReadOnlyList<ReservationPolicyPreview>> PreviewManyAsync(
        IReadOnlyList<(Guid OfferId, Guid? CategoryId)> lines,
        CancellationToken cancellationToken);
}

/// <summary>تصویر یک چرخه برای projection.</summary>
public sealed record ReservationCycleSnapshot(
    Guid CycleId,
    Guid CheckoutId,
    int CycleNumber,
    ReservationCycleReason Reason,
    ReservationCycleStatus Status,
    DateTimeOffset StartedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? EndedAt,
    int EffectiveHoldMinutes,
    int EffectiveMaxCycles,
    string PolicySource,
    IReadOnlyList<Guid> ReservationIds,
    Guid? PaymentAttemptId,
    string? CorrelationId);

/// <summary>رویداد ممیزی.</summary>
public sealed record ReservationCycleEventSnapshot(
    Guid EventId,
    Guid CheckoutId,
    Guid? CycleId,
    ReservationCycleEventKind Kind,
    DateTimeOffset OccurredAt,
    string Detail);

/// <summary>تصویر فقط‌خواندنی چرخه‌های سفارش.</summary>
public sealed record ReservationCycleProjection(
    Guid CheckoutId,
    int? CurrentCycleNumber,
    ReservationCycleStatus? CurrentStatus,
    DateTimeOffset? StartedAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset ServerTime,
    int SecondsRemaining,
    int TotalCyclesCreated,
    int EffectiveMaxCycles,
    int RetryCountRemaining,
    IReadOnlyList<ReservationCycleSnapshot> History,
    string? SupplyStatus);

/// <summary>دایرکتوری چرخه رزرو سفارش.</summary>
public interface IReservationCycleDirectory
{
    /// <summary>چرخه را بدون Save جدا آماده می‌کند تا با commit سفارش یکی شود.</summary>
    global::Tooba.Order.Domain.ReservationCycle PrepareStart(
        Guid checkoutId,
        ReservationCycleReason reason,
        DateTimeOffset startedAt,
        DateTimeOffset expiresAt,
        ReservationCyclePolicySnapshot policy,
        IReadOnlyList<Guid> reservationIds,
        string? actor,
        string? correlationId,
        Guid? paymentAttemptId);

    /// <summary>چرخه را می‌سازد و ذخیره می‌کند.</summary>
    Task<ReservationCycleSnapshot> StartAsync(
        Guid checkoutId,
        ReservationCycleReason reason,
        DateTimeOffset startedAt,
        DateTimeOffset expiresAt,
        ReservationCyclePolicySnapshot policy,
        IReadOnlyList<Guid> reservationIds,
        string? actor,
        string? correlationId,
        Guid? paymentAttemptId,
        CancellationToken cancellationToken);

    /// <summary>تلاش پرداخت داخل چرخه فعال مهلت را تمدید نمی‌کند.</summary>
    Task CorrelatePaymentAttemptAsync(Guid checkoutId, Guid? paymentAttemptId, CancellationToken cancellationToken);

    /// <summary>گذار بازبینی دستی روی همان چرخه.</summary>
    Task TransitionManualReviewAsync(
        Guid checkoutId,
        DateTimeOffset reviewExpiresAt,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    /// <summary>چرخه‌های سررسید را می‌بندد.</summary>
    Task<int> CloseExpiredDueAsync(DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>چرخه فعال سفارش را می‌بندد.</summary>
    Task CloseActiveAsync(
        Guid checkoutId,
        ReservationCycleStatus status,
        DateTimeOffset endedAt,
        CancellationToken cancellationToken);

    /// <summary>درخواست بازتملک را ثبت می‌کند؛ شمارهٔ چرخه نمی‌سازد.</summary>
    Task RecordReacquireRequestedAsync(Guid checkoutId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>شکست بازتملک بدون مصرف شماره.</summary>
    Task RecordReacquireFailedAsync(Guid checkoutId, DateTimeOffset now, string detail, CancellationToken cancellationToken);

    /// <summary>سقف retry.</summary>
    Task RecordRetryLimitReachedAsync(Guid checkoutId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>چرخه فعال در صورت وجود.</summary>
    Task<ReservationCycleSnapshot?> GetActiveAsync(Guid checkoutId, CancellationToken cancellationToken);

    /// <summary>تعداد چرخه‌های شماره‌دار (شکست بازتملک حساب نمی‌شود).</summary>
    Task<int> CountCreatedAsync(Guid checkoutId, CancellationToken cancellationToken);

    /// <summary>تصویر بدون mutation.</summary>
    Task<ReservationCycleProjection> GetProjectionAsync(
        Guid checkoutId,
        DateTimeOffset serverNow,
        string? supplyStatus,
        CancellationToken cancellationToken);

    /// <summary>تصویر دسته‌ای بدون N+1.</summary>
    Task<IReadOnlyDictionary<Guid, ReservationCycleProjection>> GetProjectionsAsync(
        IReadOnlyList<Guid> checkoutIds,
        DateTimeOffset serverNow,
        IReadOnlyDictionary<Guid, string?>? supplyByCheckout,
        CancellationToken cancellationToken);

    /// <summary>رویدادهای ممیزی به ترتیب زمان.</summary>
    Task<IReadOnlyList<ReservationCycleEventSnapshot>> ListEventsAsync(
        Guid checkoutId,
        CancellationToken cancellationToken);
}

/// <summary>کد پایدار سقف retry.</summary>
public static class ReservationCycleErrors
{
    /// <summary>کد ماشین.</summary>
    public const string RetryLimitReached = "inventory.reservation.retry_limit_reached";

    /// <summary>پیام مشتری.</summary>
    public const string RetryLimitReachedFa =
        "تعداد دفعات مجاز رزرو مجدد موجودی برای این سفارش به پایان رسیده است.";
}
