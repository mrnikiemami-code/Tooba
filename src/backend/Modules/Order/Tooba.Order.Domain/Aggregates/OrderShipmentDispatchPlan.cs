using Tooba.BuildingBlocks;

namespace Tooba.Order.Domain.Aggregates;

/// <summary>گونهٔ تلاش فیزیکی که یک برنامهٔ ارسال مستقل به آن تعلق دارد.</summary>
public static class OrderShipmentAttemptKinds
{
    /// <summary>ارسال مجدد از ظرفیت بازگشتی سالم همان مرسولهٔ مبدأ.</summary>
    public const string Resend = "resend";

    /// <summary>جایگزینی کالای آسیب‌دیده از موجودی سالم با رزرو مستقل خودش.</summary>
    public const string Replacement = "replacement";
}

/// <summary>منبع اثبات «آماده بودن فیزیکی» یک تلاش جدید (برچسب پایدار، نه متن کاربر).</summary>
public static class OrderShipmentReadinessSources
{
    /// <summary>رسید فیزیکی دریافت در مبدأ همان مقدار سالم قابل‌ارسال.</summary>
    public const string ReturnReceipt = "return_receipt";

    /// <summary>رزرو هدف‌دار موجودی سالمِ همان تصمیم جایگزینی.</summary>
    public const string ReplacementHold = "replacement_hold";

    /// <summary>خلق موفق مرسوله توسط همان تصمیم ثبت‌شده (شاهد محافظه‌کارانه، فقط وقتی مالک اثبات کرده است).</summary>
    public const string ParcelCreated = "parcel_created";
}

/// <summary>
/// TB-270-R2: تأییدیهٔ تغییرناپذیر برنامهٔ ارسال <b>یک مرسولهٔ فیزیکی موجود</b> که Order مالک آن است.
/// </summary>
/// <remarks>
/// <para>
/// این رکورد فقط یک واقعیت Order است و هیچ مرسوله/رزرو/تصمیمی نمی‌سازد: مرسوله پیش‌تر توسط Fulfillment در
/// مرحلهٔ تخصیص ساخته شده و اینجا فقط زمان‌بندی <b>همان</b> مرسوله ثبت می‌شود. یک ردیف به ازای هر مرسوله
/// (کلید یکتای <see cref="ShipmentId"/>) و تغییرناپذیر؛ بازپخش همان قصد همان ردیف را برمی‌گرداند و قصد
/// متفاوت یک تعارض نوع‌دار است، نه بازنویسی.
/// </para>
/// <para>
/// ورودی‌های محاسبه (شناسهٔ capture/بازبینی/نسخهٔ قالب/گروه/ردیف/durations/سیاست) عیناً ذخیره می‌شوند تا
/// همان برنامه با همان نسخهٔ سیاست قابل بازتولید باشد؛ هیچ snapshot جاری یا تنظیم mutable کپی نمی‌شود.
/// </para>
/// </remarks>
public sealed class OrderShipmentDispatchPlan
{
    /// <summary>بیشینهٔ طول اثر انگشت مقدار/خط منبع.</summary>
    public const int MaxFingerprintLength = 512;

    /// <summary>بیشینهٔ طول کلید رخداد بازبینی.</summary>
    public const int MaxOccurrenceKeyLength = 128;

    /// <summary>بیشینهٔ طول کد روش/خدمت/گزینه.</summary>
    public const int MaxCodeLength = 128;

    /// <summary>بیشینهٔ طول شناسهٔ منطقهٔ زمانی.</summary>
    public const int MaxTimeZoneLength = 64;

    /// <summary>بالاترین تیک یک روز کامل.</summary>
    public const long MaxTimeOfDayTicks = TimeSpan.TicksPerDay - 1;

    /// <summary>سازندهٔ EF.</summary>
    private OrderShipmentDispatchPlan()
    {
    }

    /// <summary>هویت پایدار این تأییدیه و کلید بازپخش.</summary>
    public Guid PlanId { get; private set; }

    /// <summary>مرسولهٔ فیزیکی موجود که این برنامه به آن bind شده است (یکتا).</summary>
    public Guid ShipmentId { get; private set; }

    /// <summary>سفارش مشتری مالک.</summary>
    public Guid CheckoutId { get; private set; }

    /// <summary>سفارش فروشندهٔ مالک.</summary>
    public Guid SellerOrderId { get; private set; }

    /// <summary>واحد انجام مالک.</summary>
    public Guid FulfillmentId { get; private set; }

    /// <summary>گروه ارسال پذیرفته‌شدهٔ همین تلاش.</summary>
    public Guid ShippingGroupId { get; private set; }

    /// <summary>گونهٔ تلاش (<see cref="OrderShipmentAttemptKinds"/>).</summary>
    public string AttemptKind { get; private set; } = string.Empty;

    /// <summary>مرسولهٔ مبدأ (بازگشتی/آسیب‌دیده) یا تهی.</summary>
    public Guid? SourceShipmentId { get; private set; }

    /// <summary>تصمیم کالای آسیب‌دیده که این جایگزینی را تأمین می‌کند یا تهی.</summary>
    public Guid? DamagedResolutionId { get; private set; }

    /// <summary>رزرو هدف‌دار موجودی سالمِ همان جایگزینی یا تهی.</summary>
    public Guid? ReplacementReservationId { get; private set; }

    /// <summary>اثر انگشت دقیق خط/مقدار منبع در لحظهٔ تأیید (برای تطبیق با مرسولهٔ فیزیکی).</summary>
    public string SourceFingerprint { get; private set; } = string.Empty;

    /// <summary>گونهٔ مالک ارسال (عددی، هم‌معنا با شاهد پذیرفته‌شده).</summary>
    public int OwnerKind { get; private set; }

    /// <summary>tenant مالک Store یا تهی.</summary>
    public string? OwnerTenantKey { get; private set; }

    /// <summary>فروشندهٔ مالک Seller یا تهی.</summary>
    public Guid? OwnerSellerPartyId { get; private set; }

    /// <summary>کد روش ارسال همان گروه.</summary>
    public string MethodCode { get; private set; } = string.Empty;

    /// <summary>برچسب روش ارسال همان گروه.</summary>
    public string MethodLabel { get; private set; } = string.Empty;

    /// <summary>کد خدمت capture‌شده.</summary>
    public string ServiceCode { get; private set; } = string.Empty;

    /// <summary>گزینهٔ capture‌شده یا تهی.</summary>
    public string? OptionCode { get; private set; }

    /// <summary>شناسهٔ snapshot تغییرناپذیر Shipping.</summary>
    public Guid SnapshotId { get; private set; }

    /// <summary>پیش‌نویس authoring مبدأ.</summary>
    public Guid DraftId { get; private set; }

    /// <summary>بازبینی مبدأ همان snapshot.</summary>
    public long SourceRevision { get; private set; }

    /// <summary>نسخهٔ قالب capture.</summary>
    public int FormatVersion { get; private set; }

    /// <summary>نسخهٔ انتخاب publication.</summary>
    public long SelectionVersion { get; private set; }

    /// <summary>گروه جغرافیایی Shipping.</summary>
    public Guid AreaGroupId { get; private set; }

    /// <summary>ردیف نرخ Shipping.</summary>
    public Guid RateRowId { get; private set; }

    /// <summary>تاریخ تعهد تحویل مشتری.</summary>
    public DateOnly CustomerReceivingDate { get; private set; }

    /// <summary>تیک شروع بازهٔ تعهد مشتری.</summary>
    public long CustomerReceivingOpenTicks { get; private set; }

    /// <summary>تیک پایان بازهٔ تعهد مشتری (نیم‌باز).</summary>
    public long CustomerReceivingCloseTicks { get; private set; }

    /// <summary>منطقهٔ زمانی تعهد مشتری.</summary>
    public string CustomerReceivingTimeZone { get; private set; } = string.Empty;

    /// <summary>تاریخ محلی پذیرش حامل.</summary>
    public DateOnly HandoffDate { get; private set; }

    /// <summary>تیک شروع پنجرهٔ پذیرش حامل.</summary>
    public long HandoffOpenTicks { get; private set; }

    /// <summary>تیک پایان پنجرهٔ پذیرش حامل (نیم‌باز؛ خودِ لحظه دیر است).</summary>
    public long HandoffCloseTicks { get; private set; }

    /// <summary>منطقهٔ زمانی حامل.</summary>
    public string HandoffTimeZone { get; private set; } = string.Empty;

    /// <summary>آخرین لحظهٔ مجاز <b>شروع</b> آماده‌سازی (نه پایان بسته‌بندی).</summary>
    public DateTimeOffset PreparationStartUtc { get; private set; }

    /// <summary>آخرین لحظهٔ حرکت به‌سوی حامل.</summary>
    public DateTimeOffset TransferDepartureUtc { get; private set; }

    /// <summary>لحظهٔ بسته‌شدن پنجرهٔ پذیرش حامل (شامل نیست).</summary>
    public DateTimeOffset HandoffDeadlineUtc { get; private set; }

    /// <summary>دقیقهٔ آماده‌سازی مصرف‌شده (یک‌بار).</summary>
    public int PreparationMinutes { get; private set; }

    /// <summary>دقیقهٔ انتقال به حامل مصرف‌شده (یک‌بار).</summary>
    public int HandoffMinutes { get; private set; }

    /// <summary>روزهای کاری حمل همان ردیف پذیرفته‌شده.</summary>
    public int TransportWorkingDays { get; private set; }

    /// <summary>نسخهٔ سیاست زمان‌بندی که این برنامه با آن ساخته شد.</summary>
    public int PolicyVersion { get; private set; }

    /// <summary>منبع اثبات آماده‌بودن فیزیکی (<see cref="OrderShipmentReadinessSources"/>).</summary>
    public string ReadinessSource { get; private set; } = string.Empty;

    /// <summary>لحظهٔ اثبات‌شدهٔ آماده‌بودن منبع.</summary>
    public DateTimeOffset ReadinessAtUtc { get; private set; }

    /// <summary>کلید رخداد بازبینی/تأیید اپراتور برای تشخیص بازپخش از قصد جدید.</summary>
    public string OccurrenceKey { get; private set; } = string.Empty;

    /// <summary>تأییدکنندهٔ سرور (هرگز boolean مرورگر).</summary>
    public Guid ConfirmedByUserId { get; private set; }

    /// <summary>لحظهٔ تأیید سرور.</summary>
    public DateTimeOffset ConfirmedAtUtc { get; private set; }

    /// <summary>
    /// یک تأییدیهٔ تغییرناپذیر می‌سازد. هر مقدار ناقص/ناسازگار پیش از هر نوشتن رد می‌شود؛ هیچ شناسه،
    /// ساعت، منطقه یا موعدی جعل/گرد/clamp نمی‌شود.
    /// </summary>
    public static OrderShipmentDispatchPlan Create(
        Guid planId,
        Guid shipmentId,
        Guid checkoutId,
        Guid sellerOrderId,
        Guid fulfillmentId,
        Guid shippingGroupId,
        string attemptKind,
        Guid? sourceShipmentId,
        Guid? damagedResolutionId,
        Guid? replacementReservationId,
        string sourceFingerprint,
        int ownerKind,
        string? ownerTenantKey,
        Guid? ownerSellerPartyId,
        string methodCode,
        string methodLabel,
        string serviceCode,
        string? optionCode,
        Guid snapshotId,
        Guid draftId,
        long sourceRevision,
        int formatVersion,
        long selectionVersion,
        Guid areaGroupId,
        Guid rateRowId,
        DateOnly customerReceivingDate,
        long customerReceivingOpenTicks,
        long customerReceivingCloseTicks,
        string customerReceivingTimeZone,
        DateOnly handoffDate,
        long handoffOpenTicks,
        long handoffCloseTicks,
        string handoffTimeZone,
        DateTimeOffset preparationStartUtc,
        DateTimeOffset transferDepartureUtc,
        DateTimeOffset handoffDeadlineUtc,
        int preparationMinutes,
        int handoffMinutes,
        int transportWorkingDays,
        int policyVersion,
        string readinessSource,
        DateTimeOffset readinessAtUtc,
        string occurrenceKey,
        Guid confirmedByUserId,
        DateTimeOffset confirmedAtUtc)
    {
        if (planId == Guid.Empty || shipmentId == Guid.Empty || checkoutId == Guid.Empty
            || sellerOrderId == Guid.Empty || fulfillmentId == Guid.Empty || shippingGroupId == Guid.Empty)
        {
            throw new ContractOperationException("order.shipment_plan.invalid");
        }

        var kind = Normalize(attemptKind, 16, "order.shipment_plan.invalid");
        if (kind is not (OrderShipmentAttemptKinds.Resend or OrderShipmentAttemptKinds.Replacement))
        {
            throw new ContractOperationException("order.shipment_plan.invalid");
        }

        // هر گونه تلاش، دقیقاً یک شاهد منبع خودش را دارد: ارسال مجدد از مرسولهٔ مبدأ، جایگزینی از تصمیم آسیب‌دیده.
        if (kind == OrderShipmentAttemptKinds.Resend && (sourceShipmentId is null || damagedResolutionId is not null))
        {
            throw new ContractOperationException("order.shipment_plan.invalid");
        }

        if (kind == OrderShipmentAttemptKinds.Replacement && damagedResolutionId is null)
        {
            throw new ContractOperationException("order.shipment_plan.invalid");
        }

        var fingerprint = Normalize(sourceFingerprint, MaxFingerprintLength, "order.shipment_plan.invalid");
        if (ownerKind is < 0 or > 2)
        {
            throw new ContractOperationException("order.shipment_plan.invalid");
        }

        var method = Normalize(methodCode, MaxCodeLength, "order.shipment_plan.invalid");
        var service = Normalize(serviceCode, MaxCodeLength, "order.shipment_plan.invalid");
        var option = string.IsNullOrWhiteSpace(optionCode)
            ? null
            : Normalize(optionCode, MaxCodeLength, "order.shipment_plan.invalid");
        var label = string.IsNullOrWhiteSpace(methodLabel) ? method : methodLabel.Trim();

        if (snapshotId == Guid.Empty || draftId == Guid.Empty || sourceRevision <= 0
            || formatVersion <= 0 || selectionVersion <= 0 || areaGroupId == Guid.Empty || rateRowId == Guid.Empty)
        {
            throw new ContractOperationException("order.shipment_plan.invalid");
        }

        if (customerReceivingDate == default
            || customerReceivingOpenTicks < 0 || customerReceivingOpenTicks > MaxTimeOfDayTicks
            || customerReceivingCloseTicks <= customerReceivingOpenTicks
            || customerReceivingCloseTicks > MaxTimeOfDayTicks)
        {
            throw new ContractOperationException("order.shipment_plan.invalid");
        }

        if (handoffDate == default
            || handoffOpenTicks < 0 || handoffOpenTicks > MaxTimeOfDayTicks
            || handoffCloseTicks <= handoffOpenTicks
            || handoffCloseTicks > MaxTimeOfDayTicks
            || handoffDate > customerReceivingDate)
        {
            throw new ContractOperationException("order.shipment_plan.invalid");
        }

        var customerZone = Normalize(customerReceivingTimeZone, MaxTimeZoneLength, "order.shipment_plan.invalid");
        var handoffZone = Normalize(handoffTimeZone, MaxTimeZoneLength, "order.shipment_plan.invalid");

        if (preparationMinutes < 0 || handoffMinutes < 0 || transportWorkingDays < 0 || policyVersion <= 0)
        {
            throw new ContractOperationException("order.shipment_plan.invalid");
        }

        // سه موعد مستقل: شروع آماده‌سازی ≤ حرکت ≤ بسته‌شدن پنجرهٔ پذیرش (خودِ لحظهٔ بسته‌شدن «دیر» است).
        if (!(preparationStartUtc <= transferDepartureUtc
              && transferDepartureUtc <= handoffDeadlineUtc
              && preparationStartUtc <= handoffDeadlineUtc))
        {
            throw new ContractOperationException("order.shipment_plan.invalid");
        }

        var readiness = Normalize(readinessSource, 32, "order.shipment_plan.invalid");
        if (readiness is not (OrderShipmentReadinessSources.ReturnReceipt
            or OrderShipmentReadinessSources.ReplacementHold
            or OrderShipmentReadinessSources.ParcelCreated))
        {
            throw new ContractOperationException("order.shipment_plan.invalid");
        }

        if (readinessAtUtc == default || confirmedAtUtc == default || confirmedByUserId == Guid.Empty)
        {
            throw new ContractOperationException("order.shipment_plan.invalid");
        }

        var occurrence = Normalize(occurrenceKey, MaxOccurrenceKeyLength, "order.shipment_plan.invalid");

        return new OrderShipmentDispatchPlan
        {
            PlanId = planId,
            ShipmentId = shipmentId,
            CheckoutId = checkoutId,
            SellerOrderId = sellerOrderId,
            FulfillmentId = fulfillmentId,
            ShippingGroupId = shippingGroupId,
            AttemptKind = kind,
            SourceShipmentId = sourceShipmentId,
            DamagedResolutionId = damagedResolutionId,
            ReplacementReservationId = replacementReservationId,
            SourceFingerprint = fingerprint,
            OwnerKind = ownerKind,
            OwnerTenantKey = string.IsNullOrWhiteSpace(ownerTenantKey) ? null : ownerTenantKey.Trim(),
            OwnerSellerPartyId = ownerSellerPartyId,
            MethodCode = method,
            MethodLabel = label,
            ServiceCode = service,
            OptionCode = option,
            SnapshotId = snapshotId,
            DraftId = draftId,
            SourceRevision = sourceRevision,
            FormatVersion = formatVersion,
            SelectionVersion = selectionVersion,
            AreaGroupId = areaGroupId,
            RateRowId = rateRowId,
            CustomerReceivingDate = customerReceivingDate,
            CustomerReceivingOpenTicks = customerReceivingOpenTicks,
            CustomerReceivingCloseTicks = customerReceivingCloseTicks,
            CustomerReceivingTimeZone = customerZone,
            HandoffDate = handoffDate,
            HandoffOpenTicks = handoffOpenTicks,
            HandoffCloseTicks = handoffCloseTicks,
            HandoffTimeZone = handoffZone,
            PreparationStartUtc = preparationStartUtc.ToUniversalTime(),
            TransferDepartureUtc = transferDepartureUtc.ToUniversalTime(),
            HandoffDeadlineUtc = handoffDeadlineUtc.ToUniversalTime(),
            PreparationMinutes = preparationMinutes,
            HandoffMinutes = handoffMinutes,
            TransportWorkingDays = transportWorkingDays,
            PolicyVersion = policyVersion,
            ReadinessSource = readiness,
            ReadinessAtUtc = readinessAtUtc.ToUniversalTime(),
            OccurrenceKey = occurrence,
            ConfirmedByUserId = confirmedByUserId,
            ConfirmedAtUtc = confirmedAtUtc.ToUniversalTime(),
        };
    }

    /// <summary>
    /// آیا همان قصد بازبینی دقیقاً همین ردیف را می‌خواهد (بازپخش) — مقایسهٔ همهٔ ورودی‌های معنادار، بدون
    /// بازمحاسبهٔ هیچ موعدی.
    /// </summary>
    /// <param name="other">قصد جدید.</param>
    public bool HasSameIntent(OrderShipmentDispatchPlan other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return ShipmentId == other.ShipmentId
            && ShippingGroupId == other.ShippingGroupId
            && string.Equals(AttemptKind, other.AttemptKind, StringComparison.Ordinal)
            && SourceShipmentId == other.SourceShipmentId
            && DamagedResolutionId == other.DamagedResolutionId
            && ReplacementReservationId == other.ReplacementReservationId
            && string.Equals(SourceFingerprint, other.SourceFingerprint, StringComparison.Ordinal)
            && string.Equals(OccurrenceKey, other.OccurrenceKey, StringComparison.Ordinal)
            && CustomerReceivingDate == other.CustomerReceivingDate
            && CustomerReceivingOpenTicks == other.CustomerReceivingOpenTicks
            && CustomerReceivingCloseTicks == other.CustomerReceivingCloseTicks
            && string.Equals(CustomerReceivingTimeZone, other.CustomerReceivingTimeZone, StringComparison.Ordinal)
            && HandoffDate == other.HandoffDate
            && HandoffOpenTicks == other.HandoffOpenTicks
            && HandoffCloseTicks == other.HandoffCloseTicks
            && string.Equals(HandoffTimeZone, other.HandoffTimeZone, StringComparison.Ordinal)
            && PreparationStartUtc == other.PreparationStartUtc
            && TransferDepartureUtc == other.TransferDepartureUtc
            && HandoffDeadlineUtc == other.HandoffDeadlineUtc
            && PreparationMinutes == other.PreparationMinutes
            && HandoffMinutes == other.HandoffMinutes
            && TransportWorkingDays == other.TransportWorkingDays
            && PolicyVersion == other.PolicyVersion
            && SnapshotId == other.SnapshotId
            && DraftId == other.DraftId
            && SourceRevision == other.SourceRevision
            && FormatVersion == other.FormatVersion
            && AreaGroupId == other.AreaGroupId
            && RateRowId == other.RateRowId;
    }

    private static string Normalize(string? value, int maxLength, string faultCode)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length == 0 || trimmed.Length > maxLength)
        {
            throw new ContractOperationException(faultCode);
        }

        return trimmed;
    }
}
