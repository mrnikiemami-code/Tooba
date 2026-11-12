using Tooba.BuildingBlocks;

namespace Tooba.Order.Domain.Checkout;

/// <summary>
/// گونهٔ مالکِ منطقی ارسال که شاهد فشرده برایش پذیرفته شده است.
/// </summary>
/// <remarks>
/// این هویت <b>داده</b> است، نه مجوز actor، و هرگز از کاربر، اشاره‌گر فعلی یا edition حدس زده نمی‌شود.
/// نام‌ها عیناً با معنای منبع capture هم‌خوان‌اند: فروشگاه (tenant), مارکت‌پلیس (بدون tenant/فروشنده)
/// و فروشندهٔ واقعی (بدون tenant). گروه ارسال همچنان فروشندهٔ فروشندهٔ خود را جدا نگه می‌دارد.
/// </remarks>
public enum ShippingGroupOwnerKind
{
    /// <summary>فروشگاه SingleStore با tenant پایدار و بدون فروشنده.</summary>
    Store = 0,

    /// <summary>نصب Marketplace بدون tenant فروشگاه و بدون فروشنده.</summary>
    Marketplace = 1,

    /// <summary>یک فروشندهٔ Marketplace با شناسهٔ واقعی و بدون tenant.</summary>
    Seller = 2,
}

/// <summary>
/// هویت کامل مالک ارسال: گونه + tenant اختیاری + فروشندهٔ اختیاری، دقیقاً یکی از سه شکل مجاز.
/// </summary>
/// <remarks>
/// هیچ حالت «مالک نامعلوم» پذیرفته نمی‌شود؛ اگر شاهد وجود دارد، مالک هم باید کامل باشد.
/// مقایسهٔ tenant دقیق و حساس به بزرگی حروف است (هرگز lower/نرمال نمی‌شود).
/// </remarks>
public sealed class ShippingGroupOwner
{
    /// <summary>حداکثر طول کلید tenant.</summary>
    public const int MaxTenantKeyLength = 64;

    /// <summary>سازندهٔ EF.</summary>
    private ShippingGroupOwner()
    {
    }

    /// <summary>گونهٔ مالک.</summary>
    public ShippingGroupOwnerKind Kind { get; private set; }

    /// <summary>tenant مالک Store؛ در دو گونهٔ دیگر تهی.</summary>
    public string? TenantKey { get; private set; }

    /// <summary>فروشندهٔ واقعی مالک Seller؛ در دو گونهٔ دیگر تهی.</summary>
    public Guid? SellerPartyId { get; private set; }

    /// <summary>مالک فروشگاه با tenant پایدار.</summary>
    public static ShippingGroupOwner Store(string tenantKey)
    {
        var key = (tenantKey ?? string.Empty).Trim();
        if (key.Length == 0 || key.Length > MaxTenantKeyLength)
        {
            throw new ContractOperationException(ShippingGroupEvidenceFault.OwnerInvalid);
        }

        return new ShippingGroupOwner
        {
            Kind = ShippingGroupOwnerKind.Store,
            TenantKey = key,
            SellerPartyId = null,
        };
    }

    /// <summary>مالک مارکت‌پلیس؛ بدون tenant و بدون فروشنده.</summary>
    public static ShippingGroupOwner Marketplace() =>
        new()
        {
            Kind = ShippingGroupOwnerKind.Marketplace,
            TenantKey = null,
            SellerPartyId = null,
        };

    /// <summary>مالک فروشندهٔ واقعی؛ بدون tenant.</summary>
    public static ShippingGroupOwner Seller(Guid sellerPartyId)
    {
        if (sellerPartyId == Guid.Empty)
        {
            throw new ContractOperationException(ShippingGroupEvidenceFault.OwnerInvalid);
        }

        return new ShippingGroupOwner
        {
            Kind = ShippingGroupOwnerKind.Seller,
            TenantKey = null,
            SellerPartyId = sellerPartyId,
        };
    }

    /// <summary>
    /// مالک persist‌شده را بازمی‌سازد و شکل ناقص/ناسازگار را رد می‌کند (هیچ heuristic upgrade).
    /// </summary>
    public static ShippingGroupOwner Restore(
        ShippingGroupOwnerKind kind,
        string? tenantKey,
        Guid? sellerPartyId)
    {
        var tenant = string.IsNullOrWhiteSpace(tenantKey) ? null : tenantKey.Trim();
        var seller = sellerPartyId is Guid id && id != Guid.Empty ? id : (Guid?)null;

        if (sellerPartyId is Guid declaredEmpty && declaredEmpty == Guid.Empty)
        {
            throw new ContractOperationException(ShippingGroupEvidenceFault.OwnerInvalid);
        }

        return (kind, tenant, seller) switch
        {
            (ShippingGroupOwnerKind.Store, { Length: > 0 and <= MaxTenantKeyLength }, null) =>
                Store(tenant!),
            (ShippingGroupOwnerKind.Marketplace, null, null) => Marketplace(),
            (ShippingGroupOwnerKind.Seller, null, { } owner) => Seller(owner),
            _ => throw new ContractOperationException(ShippingGroupEvidenceFault.OwnerInvalid),
        };
    }

    /// <summary>
    /// مالک با گروه می‌خواند: گونهٔ Seller باید همان فروشندهٔ گروه باشد و Store/Marketplace هرگز
    /// به‌جای فروشنده بازنویسی نشوند.
    /// </summary>
    public void EnsureCoherentWithGroupSeller(Guid groupSellerPartyId)
    {
        if (Kind == ShippingGroupOwnerKind.Seller)
        {
            if (SellerPartyId != groupSellerPartyId)
            {
                throw new ContractOperationException(ShippingGroupEvidenceFault.OwnerMismatch);
            }

            return;
        }

        if (SellerPartyId is not null)
        {
            throw new ContractOperationException(ShippingGroupEvidenceFault.OwnerInvalid);
        }
    }
}

/// <summary>
/// شاهد فشردهٔ پذیرفته‌شدهٔ یک گروه ارسال: هویت کامل مالک، ارجاع‌های پایدار snapshot و
/// مبالغ/دریافت نهاییِ همان گروه با دقت کامل.
/// </summary>
/// <remarks>
/// <para>
/// این مقدار عمداً «فشرده» است: هیچ payload خام، کل هفتهٔ کاری، مسیر جغرافیایی، همهٔ شرایط
/// تعرفه یا ترجمه‌ها را در خود ندارد؛ اندازهٔ آن ثابت و مستقل از تعداد خطوط سبد/سفارش است.
/// snapshot مشترک یک‌بار در Shipping نگه داشته می‌شود و هر سفارش فقط به آن ارجاع می‌دهد.
/// </para>
/// <para>
/// مقدار null برای گروه‌های قدیمی معتبر است (همه‌یا‌هیچ: یا کامل یا غایب، نه ناقص).
/// سازنده فقط برای ترکیب trusted سمت سرور است؛ هیچ transport/درخواست HTTP این شیء را از
/// مرورگر نمی‌پذیرد و هیچ مبلغ/مدتی از روی تعرفهٔ فعلی بازمحاسبه نمی‌شود.
/// </para>
/// </remarks>
public sealed class ShippingGroupAcceptedEvidence
{
    /// <summary>نسخهٔ پشتیبانی‌شدهٔ قالب capture؛ نسخهٔ ناشناس رد می‌شود (بدون fallback).</summary>
    public const int SupportedFormatVersion = 1;

    /// <summary>
    /// TB-241: نسخهٔ قالبی که مدت نوبت تحویل را نیز در بر می‌گیرد. هر دو نسخه فقط برای خواندن/پذیرش
    /// پشتیبانی می‌شوند؛ مقدار ناشناس همچنان رد می‌شود.
    /// </summary>
    public const int SlotDurationFormatVersion = 2;

    /// <summary>
    /// TB-256: نسخهٔ قالبی که تقویم مستقل پذیرش حامل را نیز در بر می‌گیرد. فقط برای خواندن/پذیرش
    /// پشتیبانی می‌شود و هیچ ردیف قدیمی با آن بازنویسی نمی‌شود.
    /// </summary>
    public const int CarrierScheduleFormatVersion = 3;

    /// <summary>آیا این نسخهٔ قالب شاهد قابل پذیرش است (فقط ۱، ۲ و ۳).</summary>
    /// <param name="formatVersion">نسخهٔ قالب.</param>
    public static bool IsSupportedFormat(int formatVersion)
        => formatVersion is SupportedFormatVersion or SlotDurationFormatVersion or CarrierScheduleFormatVersion;

    /// <summary>حداکثر طول کد ارز.</summary>
    public const int MaxCurrencyLength = 3;

    /// <summary>حداکثر طول شناسهٔ منطقهٔ زمانی.</summary>
    public const int MaxTimeZoneLength = 64;

    /// <summary>بالاترین مقدار تیکِ یک روز کامل (TimeOnly.Ticks در بازهٔ [0, TicksPerDay-1]).</summary>
    public const long MaxTimeOfDayTicks = TimeSpan.TicksPerDay - 1;

    /// <summary>
    /// بیشترین مقدار قابل‌ذخیره در numeric(18,2) برای هر مبلغ (۱۶ رقم صحیح و ۲ رقم اعشار).
    /// مقدار بزرگ‌تر یا با اعشار دقیق‌تر باید رد شود، نه گرد.
    /// </summary>
    public const decimal MaxStorableAmount = 9_999_999_999_999_999.99m;

    /// <summary>بیشینهٔ روزهای تحویل مطابق کران پذیرفته‌شدهٔ دامنهٔ Shipping.</summary>
    public const int MaximumDeliveryDaysBound = 365;

    /// <summary>سازندهٔ EF.</summary>
    private ShippingGroupAcceptedEvidence()
    {
    }

    /// <summary>شناسهٔ snapshot تغییرناپذیر Shipping (مشترک بین سفارش‌ها).</summary>
    public Guid SnapshotId { get; private set; }

    /// <summary>شناسهٔ پیش‌نویس authoring مبدأ.</summary>
    public Guid DraftId { get; private set; }

    /// <summary>بازبینی مبدأ snapshot؛ همان دقت long منبع.</summary>
    public long SourceRevision { get; private set; }

    /// <summary>نسخهٔ قالب capture.</summary>
    public int FormatVersion { get; private set; }

    /// <summary>نسخهٔ انتخاب/اشاره‌گر publication که این شاهد از آن پذیرفته شد؛ همان دقت long منبع.</summary>
    public long SelectionVersion { get; private set; }

    /// <summary>هویت کامل مالک ارسال؛ برای شاهد موجود الزامی است.</summary>
    public ShippingGroupOwner Owner { get; private set; } = null!;

    /// <summary>شناسهٔ گروه منطقهٔ Shipping که با این تحویل تطبیق داده شد.</summary>
    public Guid AreaGroupId { get; private set; }

    /// <summary>شناسهٔ ردیف تعرفهٔ Shipping که با این تحویل تطبیق داده شد.</summary>
    public Guid RateRowId { get; private set; }

    /// <summary>شناسهٔ نوع ارسال تخصیص‌یافته (اختیاری؛ فقط برای ردیابی).</summary>
    public Guid? ShippingTypeId { get; private set; }

    /// <summary>کرایهٔ پذیرفته‌شدهٔ همین گروه (بدون بسته‌بندی).</summary>
    public decimal FreightAmount { get; private set; }

    /// <summary>هزینهٔ بسته‌بندی پذیرفته‌شدهٔ همین گروه.</summary>
    public decimal PackagingAmount { get; private set; }

    /// <summary>ارز دقیق شاهد (هم‌ارز ارز گروه).</summary>
    public string Currency { get; private set; } = string.Empty;

    /// <summary>بخشی که مشتری هنگام تسویه می‌پردازد.</summary>
    public decimal PayableAtCheckoutAmount { get; private set; }

    /// <summary>بخشی که هنگام تحویل توسط حامل دریافت می‌شود.</summary>
    public decimal CarrierCollectAmount { get; private set; }

    /// <summary>تاریخِ تحویلِ پذیرفته‌شده (همان تاریخ انتخاب‌شدهٔ گروه).</summary>
    public DateOnly ReceivingDate { get; private set; }

    /// <summary>تیکِ شروع بازهٔ ساعتی پذیرفته‌شده در همان تاریخ (0..TicksPerDay-1، دقیق و بی‌افت).</summary>
    public long ReceivingOpenTicks { get; private set; }

    /// <summary>تیکِ پایان بازهٔ ساعتی پذیرفته‌شده در همان تاریخ (بزرگ‌تر از شروع، همان‌روز).</summary>
    public long ReceivingCloseTicks { get; private set; }

    /// <summary>منطقهٔ زمانی روش که ساعت‌های بالا با آن تفسیر می‌شوند.</summary>
    public string ReceivingTimeZone { get; private set; } = string.Empty;

    /// <summary>حداقل روزهای تحویل به مشتری در زمان پذیرش (نامعلوم ⇒ null؛ فقط متادیتا).</summary>
    public int? MinimumDeliveryDays { get; private set; }

    /// <summary>حداکثر روزهای تحویل به مشتری در زمان پذیرش (نامعلوم ⇒ null؛ فقط متادیتا).</summary>
    public int? MaximumDeliveryDays { get; private set; }

    /// <summary>شاهد خنثی قیمت حامل؛ برای Manual/Free تهی است.</summary>
    public ShippingGroupAcceptedCarrierQuote? CarrierQuote { get; private set; }

    /// <summary>
    /// TB-256: برنامهٔ ارسال (پذیرش حامل) این گروه یا <c>null</c> برای گروه‌های تاریخی/بدون تقویم حامل.
    /// </summary>
    public ShippingGroupPlannedDispatch? PlannedDispatch { get; private set; }

    /// <summary>
    /// شاهد فشرده می‌سازد. هر مقدار ناقص/ناسازگار پیش از هر جهش رد می‌شود؛ هیچ شناسه‌ای
    /// ساخته نمی‌شود، هیچ مبلغی clamp/گرد نمی‌شود و هیچ منطقهٔ زمانی/مالکی جعل نمی‌شود.
    /// </summary>
    public static ShippingGroupAcceptedEvidence Create(
        Guid snapshotId,
        Guid draftId,
        long sourceRevision,
        int formatVersion,
        long selectionVersion,
        ShippingGroupOwner owner,
        Guid areaGroupId,
        Guid rateRowId,
        Guid? shippingTypeId,
        decimal freightAmount,
        decimal packagingAmount,
        string currency,
        decimal payableAtCheckoutAmount,
        decimal carrierCollectAmount,
        DateOnly receivingDate,
        long receivingOpenTicks,
        long receivingCloseTicks,
        string receivingTimeZone,
        int? minimumDeliveryDays,
        int? maximumDeliveryDays,
        ShippingGroupAcceptedCarrierQuote? carrierQuote = null,
        ShippingGroupPlannedDispatch? plannedDispatch = null)
    {
        if (snapshotId == Guid.Empty)
        {
            throw new ContractOperationException(ShippingGroupEvidenceFault.SnapshotRequired);
        }

        if (draftId == Guid.Empty)
        {
            throw new ContractOperationException(ShippingGroupEvidenceFault.DraftRequired);
        }

        if (sourceRevision <= 0)
        {
            throw new ContractOperationException(ShippingGroupEvidenceFault.SourceRevisionInvalid);
        }

        if (!IsSupportedFormat(formatVersion))
        {
            throw new ContractOperationException(ShippingGroupEvidenceFault.FormatUnsupported);
        }

        if (selectionVersion <= 0)
        {
            throw new ContractOperationException(ShippingGroupEvidenceFault.SelectionVersionInvalid);
        }

        if (owner is null)
        {
            throw new ContractOperationException(ShippingGroupEvidenceFault.OwnerRequired);
        }

        if (areaGroupId == Guid.Empty)
        {
            throw new ContractOperationException(ShippingGroupEvidenceFault.AreaGroupRequired);
        }

        if (rateRowId == Guid.Empty)
        {
            throw new ContractOperationException(ShippingGroupEvidenceFault.RateRowRequired);
        }

        if (shippingTypeId == Guid.Empty)
        {
            throw new ContractOperationException(ShippingGroupEvidenceFault.ShippingTypeInvalid);
        }

        EnsureStorableAmount(freightAmount);
        EnsureStorableAmount(packagingAmount);
        EnsureStorableAmount(payableAtCheckoutAmount);
        EnsureStorableAmount(carrierCollectAmount);

        // Conservation: هیچ طرفی بازمحاسبه نمی‌شود؛ فقط ناسازگاری رد می‌شود (این برش Manual/captured).
        if (freightAmount + packagingAmount != payableAtCheckoutAmount + carrierCollectAmount)
        {
            throw new ContractOperationException(ShippingGroupEvidenceFault.AmountConservationInvalid);
        }

        var normalizedCurrency = (currency ?? string.Empty).Trim().ToUpperInvariant();
        if (normalizedCurrency.Length != MaxCurrencyLength
            || !normalizedCurrency.All(char.IsAsciiLetterUpper))
        {
            throw new ContractOperationException(ShippingGroupEvidenceFault.CurrencyInvalid);
        }

        carrierQuote?.EnsureCoherentWithCheckoutAmounts(freightAmount, packagingAmount, normalizedCurrency);

        if (receivingDate == default)
        {
            throw new ContractOperationException(ShippingGroupEvidenceFault.ReceivingDateInvalid);
        }

        if (receivingOpenTicks < 0 || receivingOpenTicks > MaxTimeOfDayTicks
            || receivingCloseTicks < 0 || receivingCloseTicks > MaxTimeOfDayTicks
            || receivingOpenTicks >= receivingCloseTicks)
        {
            throw new ContractOperationException(ShippingGroupEvidenceFault.ReceivingIntervalInvalid);
        }

        var timeZone = (receivingTimeZone ?? string.Empty).Trim();
        if (timeZone.Length == 0 || timeZone.Length > MaxTimeZoneLength)
        {
            throw new ContractOperationException(ShippingGroupEvidenceFault.TimeZoneInvalid);
        }

        if ((minimumDeliveryDays is null) != (maximumDeliveryDays is null))
        {
            throw new ContractOperationException(ShippingGroupEvidenceFault.DeliveryDaysInvalid);
        }
        if (minimumDeliveryDays is { } min && maximumDeliveryDays is { } max
            && (min < 0 || max < min || max > MaximumDeliveryDaysBound))
        {
            throw new ContractOperationException(ShippingGroupEvidenceFault.DeliveryDaysInvalid);
        }

        // TB-256: a planned handoff can never be AFTER the accepted customer receiving date.
        if (plannedDispatch is { } dispatch && dispatch.HandoffDate > receivingDate)
        {
            throw new ContractOperationException(ShippingGroupEvidenceFault.DispatchPlanInvalid);
        }

        return new ShippingGroupAcceptedEvidence
        {
            SnapshotId = snapshotId,
            DraftId = draftId,
            SourceRevision = sourceRevision,
            FormatVersion = formatVersion,
            SelectionVersion = selectionVersion,
            Owner = owner,
            AreaGroupId = areaGroupId,
            RateRowId = rateRowId,
            ShippingTypeId = shippingTypeId,
            FreightAmount = freightAmount,
            PackagingAmount = packagingAmount,
            Currency = normalizedCurrency,
            PayableAtCheckoutAmount = payableAtCheckoutAmount,
            CarrierCollectAmount = carrierCollectAmount,
            ReceivingDate = receivingDate,
            ReceivingOpenTicks = receivingOpenTicks,
            ReceivingCloseTicks = receivingCloseTicks,
            ReceivingTimeZone = timeZone,
            MinimumDeliveryDays = minimumDeliveryDays,
            MaximumDeliveryDays = maximumDeliveryDays,
            CarrierQuote = carrierQuote,
            PlannedDispatch = plannedDispatch,
        };
    }

    /// <summary>
    /// شاهد موجود با تاریخ انتخاب‌شدهٔ گروه می‌خواند؟ (پنجرهٔ متنی گروه پارس نمی‌شود؛ برچسب انسانی نیست.)
    /// </summary>
    public void EnsureCoherentWithGroupSchedule(DateOnly? selectedDeliveryDate)
    {
        if (selectedDeliveryDate is null || selectedDeliveryDate.Value != ReceivingDate)
        {
            throw new ContractOperationException(ShippingGroupEvidenceFault.ScheduleMismatch);
        }
    }

    private static void EnsureStorableAmount(decimal amount)
    {
        if (amount < 0m)
        {
            throw new ContractOperationException(ShippingGroupEvidenceFault.AmountInvalid);
        }

        // numeric(18,2): نه گرد کردن، نه truncate — مقدار غیرقابل‌نمایش دقیقاً رد می‌شود.
        if (amount > MaxStorableAmount || amount != decimal.Round(amount, 2, MidpointRounding.ToZero))
        {
            throw new ContractOperationException(ShippingGroupEvidenceFault.AmountUnrepresentable);
        }
    }
}

/// <summary>کدهای خطای پایدار شاهد فشردهٔ گروه ارسال.</summary>
public static class ShippingGroupEvidenceFault
{
    /// <summary>snapshot بدون شناسه.</summary>
    public const string SnapshotRequired = "shipping.group.evidence.snapshot.required";

    /// <summary>پیش‌نویس بدون شناسه.</summary>
    public const string DraftRequired = "shipping.group.evidence.draft.required";

    /// <summary>بازبینی مبدأ نامعتبر.</summary>
    public const string SourceRevisionInvalid = "shipping.group.evidence.source_revision.invalid";

    /// <summary>نسخهٔ قالب پشتیبانی‌نشده.</summary>
    public const string FormatUnsupported = "shipping.group.evidence.format.unsupported";

    /// <summary>نسخهٔ انتخاب نامعتبر.</summary>
    public const string SelectionVersionInvalid = "shipping.group.evidence.selection_version.invalid";

    /// <summary>مالک ارسال الزامی است.</summary>
    public const string OwnerRequired = "shipping.group.evidence.owner.required";

    /// <summary>هویت مالک ناقص/ناسازگار است.</summary>
    public const string OwnerInvalid = "shipping.group.evidence.owner.invalid";

    /// <summary>مالک با فروشندهٔ گروه نمی‌خواند.</summary>
    public const string OwnerMismatch = "shipping.group.evidence.owner.mismatch";

    /// <summary>گروه منطقهٔ Shipping بدون شناسه.</summary>
    public const string AreaGroupRequired = "shipping.group.evidence.area_group.required";

    /// <summary>ردیف تعرفهٔ Shipping بدون شناسه.</summary>
    public const string RateRowRequired = "shipping.group.evidence.rate_row.required";

    /// <summary>شناسهٔ نوع ارسال نامعتبر.</summary>
    public const string ShippingTypeInvalid = "shipping.group.evidence.shipping_type.invalid";

    /// <summary>مبلغ منفی.</summary>
    public const string AmountInvalid = "shipping.group.evidence.amount.invalid";

    /// <summary>مبلغ با دقت/ظرفیت numeric(18,2) قابل نمایش دقیق نیست.</summary>
    public const string AmountUnrepresentable = "shipping.group.evidence.amount.unrepresentable";

    /// <summary>کرایه + بسته‌بندی با تسویه + دریافت حامل نمی‌خواند.</summary>
    public const string AmountConservationInvalid = "shipping.group.evidence.amount.conservation_invalid";

    /// <summary>ارز نامعتبر.</summary>
    public const string CurrencyInvalid = "shipping.group.evidence.currency.invalid";

    /// <summary>تاریخ دریافت نامعتبر.</summary>
    public const string ReceivingDateInvalid = "shipping.group.evidence.receiving_date.invalid";

    /// <summary>بازهٔ ساعتی دریافت نامعتبر/معکوس/چندروزه.</summary>
    public const string ReceivingIntervalInvalid = "shipping.group.evidence.receiving_interval.invalid";

    /// <summary>منطقهٔ زمانی نامعتبر.</summary>
    public const string TimeZoneInvalid = "shipping.group.evidence.time_zone.invalid";

    /// <summary>روزهای تحویل ناقص/معکوس/بیرون از کران دامنه.</summary>
    public const string DeliveryDaysInvalid = "shipping.group.evidence.delivery_days.invalid";

    /// <summary>شاهد با ارز گروه نمی‌خواند.</summary>
    public const string CurrencyMismatch = "shipping.group.evidence.currency.mismatch";

    /// <summary>شاهد با مبلغ قابل‌پرداخت هنگام تسویهٔ گروه نمی‌خواند.</summary>
    public const string PayableMismatch = "shipping.group.evidence.payable.mismatch";

    /// <summary>شاهد با تاریخ/پنجرهٔ انتخاب‌شدهٔ گروه نمی‌خواند.</summary>
    public const string ScheduleMismatch = "shipping.group.evidence.schedule.mismatch";

    /// <summary>شاهد حامل ناقص یا با مبالغ تسویه ناسازگار است.</summary>
    public const string CarrierQuoteInvalid = "shipping.group.evidence.carrier_quote.invalid";

    /// <summary>TB-256: برنامهٔ ارسال (پذیرش حامل) ناقص/ناسازگار است.</summary>
    public const string DispatchPlanInvalid = "shipping.group.evidence.dispatch_plan.invalid";
}

/// <summary>
/// TB-256: برنامهٔ <b>ارسال</b> پذیرفته‌شدهٔ یک گروه — روز و بازهٔ تحویل بسته به حامل همراه provenance
/// کامل. این یک <b>برنامه</b> است، نه رخداد: هیچ ارسال/مرسوله‌ای ساخته نمی‌شود.
/// </summary>
/// <remarks>
/// <para>
/// <b>مستقل از تحویل مشتری.</b> بازهٔ پذیرش از تقویم حامل می‌آید و تاریخ تحویل مشتری از تقویم مشتری؛
/// هیچ‌کدام جای دیگری نوشته نمی‌شود.
/// </para>
/// <para>
/// <b>قابل بازتولید.</b> نسخهٔ سیاست، تعداد روز حمل استفاده‌شده، شناسه و بازبینی snapshot مبدأ و
/// شناسهٔ منطقهٔ زمانی همان‌جا ذخیره می‌شوند تا همان محاسبه دوباره قابل اثبات باشد.
/// </para>
/// </remarks>
public sealed class ShippingGroupPlannedDispatch
{
    /// <summary>TB-264: بیشینهٔ زمان آماده‌سازی (هفت روز) — هم‌ارز کران Contracts.</summary>
    public const int MaxPreparationMinutes = 7 * 24 * 60;

    /// <summary>TB-264: بیشینهٔ زمان انتقال/تحویل به حامل (یک روز) — هم‌ارز کران Contracts.</summary>
    public const int MaxHandoffMinutes = 24 * 60;

    /// <summary>سازندهٔ EF.</summary>
    private ShippingGroupPlannedDispatch()
    {
    }

    /// <summary>روز محلی پذیرش بسته توسط حامل.</summary>
    public DateOnly HandoffDate { get; private set; }

    /// <summary>تیکِ شروع بازهٔ پذیرش همان روز.</summary>
    public long HandoffOpenTicks { get; private set; }

    /// <summary>تیکِ پایان بازهٔ پذیرش (شامل نیست).</summary>
    public long HandoffCloseTicks { get; private set; }

    /// <summary>منطقهٔ زمانی پذیرش.</summary>
    public string HandoffTimeZone { get; private set; } = string.Empty;

    /// <summary>تعداد روز کاری حمل شمرده‌شده بعد از روز پذیرش.</summary>
    public int TransportWorkingDays { get; private set; }

    /// <summary>نسخهٔ سیاست محاسبه که این برنامه را ساخت.</summary>
    public int PolicyVersion { get; private set; }

    /// <summary>شناسهٔ snapshot مبدأ (تقویم حامل استفاده‌شده).</summary>
    public Guid ScheduleSnapshotId { get; private set; }

    /// <summary>بازبینی مبدأ همان snapshot.</summary>
    public long ScheduleSourceRevision { get; private set; }

    /// <summary>
    /// TB-264: آخرین لحظهٔ شروع آماده‌سازی فیزیکی (UTC) یا تهی برای طرح‌های قدیمی/بدون سیاست زمانی.
    /// </summary>
    public DateTimeOffset? PreparationStartUtc { get; private set; }

    /// <summary>TB-264: آخرین لحظهٔ حرکت به‌سوی حامل (UTC) یا تهی.</summary>
    public DateTimeOffset? TransferDepartureUtc { get; private set; }

    /// <summary>
    /// TB-264: لحظهٔ <b>بسته‌شدن</b> پنجرهٔ پذیرش حامل (UTC، شامل نیست) یا تهی. مصرف‌کننده باید آن را
    /// «پیش از» نمایش دهد و هرگز ادعای «در همان لحظه» نسازد.
    /// </summary>
    public DateTimeOffset? HandoffDeadlineUtc { get; private set; }

    /// <summary>
    /// TB-264: زمان آماده‌سازی فیزیکی پذیرفته‌شده به دقیقه (همراه همان سیاست) یا تهی برای طرح قدیمی.
    /// </summary>
    public int? PreparationMinutes { get; private set; }

    /// <summary>TB-264: زمان انتقال/تحویل به حامل پذیرفته‌شده به دقیقه یا تهی برای طرح قدیمی.</summary>
    public int? HandoffMinutes { get; private set; }

    /// <summary>برنامهٔ ارسال را می‌سازد؛ هر مقدار ناقص/ناسازگار پیش از ذخیره رد می‌شود.</summary>
    public static ShippingGroupPlannedDispatch Create(
        DateOnly handoffDate,
        long handoffOpenTicks,
        long handoffCloseTicks,
        string handoffTimeZone,
        int transportWorkingDays,
        int policyVersion,
        Guid scheduleSnapshotId,
        long scheduleSourceRevision,
        DateTimeOffset? preparationStartUtc = null,
        DateTimeOffset? transferDepartureUtc = null,
        DateTimeOffset? handoffDeadlineUtc = null,
        int? preparationMinutes = null,
        int? handoffMinutes = null)
    {
        if (handoffDate == default
            || handoffOpenTicks < 0
            || handoffOpenTicks >= handoffCloseTicks
            || handoffCloseTicks > ShippingGroupAcceptedEvidence.MaxTimeOfDayTicks)
        {
            throw new ContractOperationException(ShippingGroupEvidenceFault.DispatchPlanInvalid);
        }

        var timeZone = (handoffTimeZone ?? string.Empty).Trim();
        if (timeZone.Length == 0 || timeZone.Length > ShippingGroupAcceptedEvidence.MaxTimeZoneLength)
        {
            throw new ContractOperationException(ShippingGroupEvidenceFault.DispatchPlanInvalid);
        }

        if (transportWorkingDays < 0
            || transportWorkingDays > ShippingGroupAcceptedEvidence.MaximumDeliveryDaysBound
            || policyVersion < 1
            || scheduleSnapshotId == Guid.Empty
            || scheduleSourceRevision <= 0)
        {
            throw new ContractOperationException(ShippingGroupEvidenceFault.DispatchPlanInvalid);
        }

        // TB-264: the three operational deadlines are an all-or-nothing triple. A policy-aware plan
        // (version 2+) MUST carry them; a legacy plan (version 1) must not pretend to have them.
        var hasAnyDeadline = preparationStartUtc is not null
                             || transferDepartureUtc is not null
                             || handoffDeadlineUtc is not null;
        var hasAllDeadlines = preparationStartUtc is not null
                              && transferDepartureUtc is not null
                              && handoffDeadlineUtc is not null;
        if (hasAnyDeadline != hasAllDeadlines
            || (policyVersion >= 2) != hasAllDeadlines)
        {
            throw new ContractOperationException(ShippingGroupEvidenceFault.DispatchPlanInvalid);
        }

        // TB-264-R1 (gap A): the three instants are NON-DECREASING. A strict ordering rejected the explicit
        // zero allowances that authoring and the forward calendar accept (0/0 and 0/30). Equality is legal
        // only where the matching allowance is zero, and the acceptance close stays exclusive.
        if (hasAllDeadlines
            && !(preparationStartUtc!.Value <= transferDepartureUtc!.Value
                 && transferDepartureUtc.Value <= handoffDeadlineUtc!.Value))
        {
            throw new ContractOperationException(ShippingGroupEvidenceFault.DispatchPlanInvalid);
        }

        // TB-264-R1: the persisted readiness INPUTS are an explicit pair too. A policy-aware plan stores both
        // allowances, a legacy plan stores neither, and the bounds are the same ones the authoring policy and
        // the scheduling authority enforce. A half pair is not a policy.
        if ((preparationMinutes is null) != (handoffMinutes is null)
            || (hasAllDeadlines && preparationMinutes is null)
            || (!hasAllDeadlines && preparationMinutes is not null)
            || preparationMinutes is < 0 or > MaxPreparationMinutes
            || handoffMinutes is < 0 or > MaxHandoffMinutes)
        {
            throw new ContractOperationException(ShippingGroupEvidenceFault.DispatchPlanInvalid);
        }

        // TB-264-R1: transfer to the carrier is ELAPSED time by definition, so the stored instants must
        // differ by exactly the stored allowance. Preparation is working time inside the configured
        // processing week and its elapsed span therefore cannot be checked here (0 minutes must be equal).
        if (hasAllDeadlines
            && (transferDepartureUtc!.Value != handoffDeadlineUtc!.Value.AddMinutes(-handoffMinutes!.Value)
                || (preparationMinutes is 0 && preparationStartUtc!.Value != transferDepartureUtc.Value)))
        {
            throw new ContractOperationException(ShippingGroupEvidenceFault.DispatchPlanInvalid);
        }

        return new ShippingGroupPlannedDispatch
        {
            HandoffDate = handoffDate,
            HandoffOpenTicks = handoffOpenTicks,
            HandoffCloseTicks = handoffCloseTicks,
            HandoffTimeZone = timeZone,
            TransportWorkingDays = transportWorkingDays,
            PolicyVersion = policyVersion,
            ScheduleSnapshotId = scheduleSnapshotId,
            ScheduleSourceRevision = scheduleSourceRevision,
            PreparationStartUtc = preparationStartUtc,
            TransferDepartureUtc = transferDepartureUtc,
            HandoffDeadlineUtc = handoffDeadlineUtc,
            PreparationMinutes = preparationMinutes,
            HandoffMinutes = handoffMinutes,
        };
    }
}

/// <summary>شاهد خنثی قیمت حامل روی گروه پذیرفته‌شده؛ Manual/Free آن را ندارند.</summary>
public sealed class ShippingGroupAcceptedCarrierQuote
{
    /// <summary>حداکثر طول کد provider/خدمت.</summary>
    public const int MaxProviderCodeLength = 64;

    /// <summary>حداکثر طول هویت ورودی نقل‌قول.</summary>
    public const int MaxIdentityLength = 128;

    /// <summary>حداکثر طول مرجع صادرشدهٔ provider.</summary>
    public const int MaxReferenceLength = 128;

    /// <summary>حداکثر طول کد مبدأ/مقصد provider.</summary>
    public const int MaxLocationCodeLength = 64;

    private ShippingGroupAcceptedCarrierQuote()
    {
    }

    /// <summary>کد provider خنثی.</summary>
    public string ProviderCode { get; private set; } = string.Empty;

    /// <summary>شناسهٔ حساب حامل مالک.</summary>
    public Guid AccountId { get; private set; }

    /// <summary>نسخهٔ پیکربندی حساب در زمان نقل‌قول.</summary>
    public int ConfigurationVersion { get; private set; }

    /// <summary>کد خدمت provider.</summary>
    public string ServiceCode { get; private set; } = string.Empty;

    /// <summary>هویت ورودی‌های نقل‌قول؛ شناسهٔ محلی provider نیست.</summary>
    public string QuoteInputIdentity { get; private set; } = string.Empty;

    /// <summary>ارز نقل‌قول.</summary>
    public string Currency { get; private set; } = string.Empty;

    /// <summary>مبلغ نهایی provider شامل اجزا.</summary>
    public decimal FinalAmount { get; private set; }

    /// <summary>جزء حمل.</summary>
    public decimal TransportationAmount { get; private set; }

    /// <summary>جزء مالیات provider؛ جدا از مالیات فروشگاه.</summary>
    public decimal TaxAmount { get; private set; }

    /// <summary>جزء بسته‌بندی provider.</summary>
    public decimal PackagingAmount { get; private set; }

    /// <summary>جزء Pickup.</summary>
    public decimal PickupAmount { get; private set; }

    /// <summary>جزء تحویل.</summary>
    public decimal DeliveryAmount { get; private set; }

    /// <summary>زمان صدور نقل‌قول.</summary>
    public DateTimeOffset IssuedAtUtc { get; private set; }

    /// <summary>پایان تازگی محلی؛ تضمین provider نیست.</summary>
    public DateTimeOffset FreshUntilUtc { get; private set; }

    /// <summary>مرجع صادرشدهٔ واقعی provider یا تهی.</summary>
    public string? ProviderReference { get; private set; }

    /// <summary>کد شهر مبدأ provider در زمان نقل‌قول.</summary>
    public string OriginLocationCode { get; private set; } = string.Empty;

    /// <summary>کد شهر مقصد provider در زمان نقل‌قول.</summary>
    public string DestinationLocationCode { get; private set; } = string.Empty;

    /// <summary>وزن بسته (گرم) در زمان نقل‌قول.</summary>
    public decimal WeightGrams { get; private set; }

    /// <summary>ارزش اظهاری بسته در زمان نقل‌قول.</summary>
    public decimal DeclaredValue { get; private set; }

    /// <summary>طول بسته (سانتی‌متر).</summary>
    public decimal LengthCentimeters { get; private set; }

    /// <summary>عرض بسته (سانتی‌متر).</summary>
    public decimal WidthCentimeters { get; private set; }

    /// <summary>ارتفاع بسته (سانتی‌متر).</summary>
    public decimal HeightCentimeters { get; private set; }

    /// <summary>شاهد حامل را می‌سازد؛ اجزای ناسازگار رد می‌شوند و شناسهٔ محلی به‌عنوان مرجع ثبت نمی‌شود.</summary>
    public static ShippingGroupAcceptedCarrierQuote Create(
        string providerCode,
        Guid accountId,
        int configurationVersion,
        string serviceCode,
        string quoteInputIdentity,
        string currency,
        decimal finalAmount,
        decimal transportationAmount,
        decimal taxAmount,
        decimal packagingAmount,
        decimal pickupAmount,
        decimal deliveryAmount,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset freshUntilUtc,
        string originLocationCode,
        string destinationLocationCode,
        decimal weightGrams,
        decimal declaredValue,
        decimal lengthCentimeters,
        decimal widthCentimeters,
        decimal heightCentimeters,
        string? providerReference)
    {
        var provider = (providerCode ?? string.Empty).Trim();
        var service = (serviceCode ?? string.Empty).Trim();
        var identity = (quoteInputIdentity ?? string.Empty).Trim();
        var normalizedCurrency = (currency ?? string.Empty).Trim().ToUpperInvariant();
        var reference = string.IsNullOrWhiteSpace(providerReference) ? null : providerReference.Trim();
        var origin = (originLocationCode ?? string.Empty).Trim();
        var destination = (destinationLocationCode ?? string.Empty).Trim();
        if (provider.Length is 0 or > MaxProviderCodeLength
            || accountId == Guid.Empty
            || configurationVersion < 1
            || service.Length is 0 or > MaxProviderCodeLength
            || identity.Length is 0 or > MaxIdentityLength
            || normalizedCurrency.Length != ShippingGroupAcceptedEvidence.MaxCurrencyLength
            || issuedAtUtc == default
            || freshUntilUtc <= issuedAtUtc
            || origin.Length is 0 or > MaxLocationCodeLength
            || destination.Length is 0 or > MaxLocationCodeLength
            || weightGrams <= 0m
            || declaredValue < 0m
            || lengthCentimeters <= 0m
            || widthCentimeters <= 0m
            || heightCentimeters <= 0m
            || (reference is { Length: > MaxReferenceLength }))
        {
            throw new ContractOperationException(ShippingGroupEvidenceFault.CarrierQuoteInvalid);
        }

        EnsureComponent(finalAmount);
        EnsureComponent(transportationAmount);
        EnsureComponent(taxAmount);
        EnsureComponent(packagingAmount);
        EnsureComponent(pickupAmount);
        EnsureComponent(deliveryAmount);
        if (finalAmount != transportationAmount + taxAmount + packagingAmount + pickupAmount + deliveryAmount)
        {
            throw new ContractOperationException(ShippingGroupEvidenceFault.CarrierQuoteInvalid);
        }

        return new ShippingGroupAcceptedCarrierQuote
        {
            ProviderCode = provider,
            AccountId = accountId,
            ConfigurationVersion = configurationVersion,
            ServiceCode = service,
            QuoteInputIdentity = identity,
            Currency = normalizedCurrency,
            FinalAmount = finalAmount,
            TransportationAmount = transportationAmount,
            TaxAmount = taxAmount,
            PackagingAmount = packagingAmount,
            PickupAmount = pickupAmount,
            DeliveryAmount = deliveryAmount,
            IssuedAtUtc = issuedAtUtc,
            FreshUntilUtc = freshUntilUtc,
            OriginLocationCode = origin,
            DestinationLocationCode = destination,
            WeightGrams = weightGrams,
            DeclaredValue = declaredValue,
            LengthCentimeters = lengthCentimeters,
            WidthCentimeters = widthCentimeters,
            HeightCentimeters = heightCentimeters,
            ProviderReference = reference,
        };
    }

    /// <summary>مبالغ شاهد باید با کرایه/بسته‌بندی/ارز تسویه یکی باشد تا بسته‌بندی دوباره جمع نشود.</summary>
    public void EnsureCoherentWithCheckoutAmounts(decimal freightAmount, decimal packagingAmount, string currency)
    {
        if (!string.Equals(Currency, currency, StringComparison.Ordinal)
            || PackagingAmount != packagingAmount
            || FinalAmount != freightAmount + packagingAmount)
        {
            throw new ContractOperationException(ShippingGroupEvidenceFault.CarrierQuoteInvalid);
        }
    }

    private static void EnsureComponent(decimal amount)
    {
        if (amount < 0m
            || amount > ShippingGroupAcceptedEvidence.MaxStorableAmount
            || amount != decimal.Round(amount, 2, MidpointRounding.ToZero))
        {
            throw new ContractOperationException(ShippingGroupEvidenceFault.CarrierQuoteInvalid);
        }
    }
}
