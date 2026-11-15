using System.Globalization;

namespace Tooba.Order.Contracts.Shipping;

/// <summary>
/// TB-270-R2: گونهٔ تلاش فیزیکی یک برنامهٔ ارسال مستقل، در شکل Contracts.
/// </summary>
/// <remarks>
/// مقادیر عیناً همان ثابت‌های دامنهٔ Order هستند (Order.Domain.Aggregates.OrderShipmentAttemptKinds) و مصرف‌کنندهٔ
/// بین‌ماژولی فقط همین شکل را می‌بیند؛ هیچ رشتهٔ فارسی/برچسبی اینجا معنا ندارد.
/// </remarks>
public static class OrderShipmentDispatchPlanKinds
{
    /// <summary>ارسال مجدد از ظرفیت بازگشتی سالم همان مرسولهٔ مبدأ.</summary>
    public const string Resend = "resend";

    /// <summary>جایگزینی کالای آسیب‌دیده از موجودی سالم با رزرو مستقل خودش.</summary>
    public const string Replacement = "replacement";
}

/// <summary>
/// TB-270-R2: یک برنامهٔ ارسال <b>مستقل و تأییدشده</b> برای یک مرسولهٔ فیزیکی موجود (ارسال مجدد یا جایگزینی).
/// </summary>
/// <remarks>
/// <para>
/// این واقعیت با شاهد پذیرفته‌شدهٔ گروه <b>اولیه</b> یکی نیست: برنامهٔ گروه توصیف تحویل سفارش اول است و هرگز
/// به تلاش جدید قرض داده نمی‌شود. این رکورد همان چیزی است که اپراتور برای این مرسولهٔ دقیق تأیید کرده و
/// تغییرناپذیر است.
/// </para>
/// <para>
/// <b>فقط Order.</b> هیچ جدولی از Fulfillment/Shipping/Inventory خوانده نمی‌شود و هیچ موعد/تاریخی بازمحاسبه
/// نمی‌شود؛ مصرف‌کننده فقط همان مقادیر ذخیره‌شده را نمایش/داوری می‌کند.
/// </para>
/// </remarks>
public sealed record OrderShipmentDispatchPlanFact(
    Guid PlanId,
    Guid ShipmentId,
    Guid CheckoutId,
    Guid SellerOrderId,
    Guid FulfillmentId,
    Guid ShippingGroupId,
    string AttemptKind,
    Guid? SourceShipmentId,
    Guid? DamagedResolutionId,
    Guid? ReplacementReservationId,
    string SourceFingerprint,
    Guid SnapshotId,
    Guid DraftId,
    long SourceRevision,
    int FormatVersion,
    long SelectionVersion,
    Guid AreaGroupId,
    Guid RateRowId,
    string MethodCode,
    string MethodLabel,
    string ServiceCode,
    string? OptionCode,
    DateOnly CustomerReceivingDate,
    long CustomerReceivingOpenTicks,
    long CustomerReceivingCloseTicks,
    string CustomerReceivingTimeZone,
    DateOnly HandoffDate,
    long HandoffOpenTicks,
    long HandoffCloseTicks,
    string HandoffTimeZone,
    DateTimeOffset PreparationStartUtc,
    DateTimeOffset TransferDepartureUtc,
    DateTimeOffset HandoffDeadlineUtc,
    int PreparationMinutes,
    int HandoffMinutes,
    int TransportWorkingDays,
    int PolicyVersion,
    string ReadinessSource,
    DateTimeOffset ReadinessAtUtc,
    string OccurrenceKey,
    Guid ConfirmedByUserId,
    DateTimeOffset ConfirmedAtUtc)
{
    /// <summary>
    /// همان برنامه در شکل «برنامهٔ ارسال گروه» برای همان مرجع خالص زمان‌بندی/صف.
    /// </summary>
    /// <remarks>
    /// معناها یکی‌اند (پنجرهٔ محلی پذیرش، نیم‌باز، سه موعد UTC، نسخهٔ سیاست)؛ فقط منبع ردیف متفاوت است.
    /// هیچ مقداری بازمحاسبه نمی‌شود.
    /// </remarks>
    public OrderAcceptedGroupDispatchPlanFact ToDispatchPlanFact()
        => new(
            SellerOrderId,
            ShippingGroupId,
            CheckoutId,
            Guid.Empty,
            MethodCode,
            MethodLabel,
            HandoffDate,
            HandoffOpenTicks,
            HandoffCloseTicks,
            HandoffTimeZone,
            TransportWorkingDays,
            PolicyVersion,
            CustomerReceivingDate,
            FormatWindow(CustomerReceivingOpenTicks, CustomerReceivingCloseTicks),
            PreparationStartUtc,
            TransferDepartureUtc,
            HandoffDeadlineUtc);

    /// <summary>بازهٔ محلی تعهد مشتری به شکل <c>HH:mm-HH:mm</c> (همان شکل موجود نمایش).</summary>
    public string CustomerReceivingTimeWindow
        => FormatWindow(CustomerReceivingOpenTicks, CustomerReceivingCloseTicks);

    private static string FormatWindow(long openTicks, long closeTicks)
    {
        var open = TimeOnly.FromTimeSpan(TimeSpan.FromTicks(openTicks));
        var close = TimeOnly.FromTimeSpan(TimeSpan.FromTicks(closeTicks));
        return string.Concat(
            open.ToString("HH\\:mm", CultureInfo.InvariantCulture),
            "-",
            close.ToString("HH\\:mm", CultureInfo.InvariantCulture));
    }
}

/// <summary>
/// TB-270-R2: خواندن فقط‌خواندنی برنامه‌های ارسال مستقل مرسوله‌ها از Order.
/// </summary>
/// <remarks>
/// هر مرسوله حداکثر یک برنامهٔ تأییدشده دارد. نبودِ ردیف یعنی «زمان‌بندی تأیید نشده» و هیچ برنامه‌ای از
/// گروه/مرسولهٔ دیگر استنتاج نمی‌شود.
/// </remarks>
public interface IOrderShipmentDispatchPlanReader
{
    /// <summary>برنامه‌های همین مرسوله‌ها (فقط ردیف‌های موجود).</summary>
    Task<IReadOnlyList<OrderShipmentDispatchPlanFact>> ListForShipmentsAsync(
        IReadOnlyList<Guid> shipmentIds,
        CancellationToken cancellationToken);

    /// <summary>برنامهٔ همین یک مرسوله یا <c>null</c> وقتی تأیید نشده است.</summary>
    Task<OrderShipmentDispatchPlanFact?> GetForShipmentAsync(
        Guid shipmentId,
        CancellationToken cancellationToken);
}
