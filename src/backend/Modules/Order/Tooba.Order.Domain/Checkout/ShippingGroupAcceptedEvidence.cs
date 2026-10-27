using Tooba.BuildingBlocks;
using Tooba.Offer.Contracts.Dtos;

namespace Tooba.Order.Domain.Checkout;

/// <summary>
/// شاهد فشردهٔ پذیرفته‌شدهٔ یک گروه ارسال: ارجاع‌های پایدار snapshot و مبالغ/دریافت نهاییِ همان گروه.
/// </summary>
/// <remarks>
/// این مقدار عمداً «فشرده» است: هیچ payload خام، کل هفتهٔ کاری، مسیر جغرافیایی، همهٔ شرایط
/// تعرفه یا ترجمه‌ها را در خود ندارد؛ اندازهٔ آن ثابت و مستقل از تعداد خطوط سبد/سفارش است.
/// snapshot مشترک یک‌بار در Shipping نگه داشته می‌شود و هر سفارش فقط به آن ارجاع می‌دهد.
/// مقدار null برای گروه‌های قدیمی معتبر است (همه‌یا‌هیچ: یا کامل یا غایب، نه ناقص).
/// سازنده فقط برای ترکیب trusted سمت سرور است؛ هیچ transport/درخواست HTTP این شیء را از
/// مرورگر نمی‌پذیرد و هیچ مبلغ/مدتی از روی تعرفهٔ فعلی بازمحاسبه نمی‌شود.
/// </remarks>
public sealed class ShippingGroupAcceptedEvidence
{
    /// <summary>نسخهٔ پشتیبانی‌شدهٔ قالب capture؛ نسخهٔ ناشناس رد می‌شود (بدون fallback).</summary>
    public const int SupportedFormatVersion = 1;

    /// <summary>حداکثر طول کد ارز.</summary>
    public const int MaxCurrencyLength = 3;

    /// <summary>حداکثر طول شناسهٔ منطقهٔ زمانی.</summary>
    public const int MaxTimeZoneLength = 64;

    /// <summary>سازندهٔ EF.</summary>
    private ShippingGroupAcceptedEvidence()
    {
    }

    /// <summary>شناسهٔ snapshot تغییرناپذیر Shipping (مشترک بین سفارش‌ها).</summary>
    public Guid SnapshotId { get; private set; }

    /// <summary>شناسهٔ پیش‌نویس authoring مبدأ.</summary>
    public Guid DraftId { get; private set; }

    /// <summary>بازبینی مبدأ snapshot.</summary>
    public int SourceRevision { get; private set; }

    /// <summary>نسخهٔ قالب capture.</summary>
    public int FormatVersion { get; private set; }

    /// <summary>نسخهٔ انتخاب/اشاره‌گر publication که این شاهد از آن پذیرفته شد.</summary>
    public int SelectionVersion { get; private set; }

    /// <summary>
    /// مسئولیت fulfilment که در زمان پذیرش واقعاً موجود بود؛ نامعلوم ⇒ null (هرگز حدس زده نمی‌شود).
    /// </summary>
    public OfferFulfillmentResponsibility? Responsibility { get; private set; }

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

    /// <summary>شروع بازهٔ دریافت موردپذیرش (تاریخ خورشیدی/Gregorian میلادی مطابق سایر ستون‌ها).</summary>
    public DateOnly ReceivingStartDate { get; private set; }

    /// <summary>پایان بازهٔ دریافت موردپذیرش.</summary>
    public DateOnly ReceivingEndDate { get; private set; }

    /// <summary>منطقهٔ زمانی روش که بازهٔ دریافت با آن تفسیر می‌شود.</summary>
    public string ReceivingTimeZone { get; private set; } = string.Empty;

    /// <summary>حداقل روزهای تحویل به مشتری در زمان پذیرش (نامعلوم ⇒ null).</summary>
    public int? MinimumDeliveryDays { get; private set; }

    /// <summary>حداکثر روزهای تحویل به مشتری در زمان پذیرش (نامعلوم ⇒ null).</summary>
    public int? MaximumDeliveryDays { get; private set; }

    /// <summary>
    /// شاهد فشرده می‌سازد. هر مقدار ناقص/ناسازگار پیش از هر جهش رد می‌شود؛ هیچ شناسه‌ای
    /// ساخته نمی‌شود، هیچ مبلغی clamp نمی‌شود و هیچ منطقهٔ زمانی پیش‌فرض جعل نمی‌شود.
    /// </summary>
    public static ShippingGroupAcceptedEvidence Create(
        Guid snapshotId,
        Guid draftId,
        int sourceRevision,
        int formatVersion,
        int selectionVersion,
        OfferFulfillmentResponsibility? responsibility,
        Guid areaGroupId,
        Guid rateRowId,
        Guid? shippingTypeId,
        decimal freightAmount,
        decimal packagingAmount,
        string currency,
        decimal payableAtCheckoutAmount,
        decimal carrierCollectAmount,
        DateOnly receivingStartDate,
        DateOnly receivingEndDate,
        string receivingTimeZone,
        int? minimumDeliveryDays,
        int? maximumDeliveryDays)
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

        if (formatVersion != SupportedFormatVersion)
        {
            throw new ContractOperationException(ShippingGroupEvidenceFault.FormatUnsupported);
        }

        if (selectionVersion <= 0)
        {
            throw new ContractOperationException(ShippingGroupEvidenceFault.SelectionVersionInvalid);
        }

        if (responsibility is { } declared
            && declared != OfferFulfillmentResponsibility.Seller
            && declared != OfferFulfillmentResponsibility.Platform)
        {
            throw new ContractOperationException(ShippingGroupEvidenceFault.ResponsibilityInvalid);
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

        if (freightAmount < 0m || packagingAmount < 0m
            || payableAtCheckoutAmount < 0m || carrierCollectAmount < 0m)
        {
            throw new ContractOperationException(ShippingGroupEvidenceFault.AmountInvalid);
        }

        var normalizedCurrency = (currency ?? string.Empty).Trim().ToUpperInvariant();
        if (normalizedCurrency.Length != MaxCurrencyLength
            || !normalizedCurrency.All(char.IsAsciiLetterUpper))
        {
            throw new ContractOperationException(ShippingGroupEvidenceFault.CurrencyInvalid);
        }

        if (receivingStartDate == default || receivingEndDate == default)
        {
            throw new ContractOperationException(ShippingGroupEvidenceFault.ReceivingIntervalInvalid);
        }

        if (receivingStartDate > receivingEndDate)
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
            && (min < 0 || max < 0 || min > max))
        {
            throw new ContractOperationException(ShippingGroupEvidenceFault.DeliveryDaysInvalid);
        }

        return new ShippingGroupAcceptedEvidence
        {
            SnapshotId = snapshotId,
            DraftId = draftId,
            SourceRevision = sourceRevision,
            FormatVersion = formatVersion,
            SelectionVersion = selectionVersion,
            Responsibility = responsibility,
            AreaGroupId = areaGroupId,
            RateRowId = rateRowId,
            ShippingTypeId = shippingTypeId,
            FreightAmount = freightAmount,
            PackagingAmount = packagingAmount,
            Currency = normalizedCurrency,
            PayableAtCheckoutAmount = payableAtCheckoutAmount,
            CarrierCollectAmount = carrierCollectAmount,
            ReceivingStartDate = receivingStartDate,
            ReceivingEndDate = receivingEndDate,
            ReceivingTimeZone = timeZone,
            MinimumDeliveryDays = minimumDeliveryDays,
            MaximumDeliveryDays = maximumDeliveryDays,
        };
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

    /// <summary>مسئولیت fulfilment نامعتبر.</summary>
    public const string ResponsibilityInvalid = "shipping.group.evidence.responsibility.invalid";

    /// <summary>گروه منطقهٔ Shipping بدون شناسه.</summary>
    public const string AreaGroupRequired = "shipping.group.evidence.area_group.required";

    /// <summary>ردیف تعرفهٔ Shipping بدون شناسه.</summary>
    public const string RateRowRequired = "shipping.group.evidence.rate_row.required";

    /// <summary>شناسهٔ نوع ارسال نامعتبر.</summary>
    public const string ShippingTypeInvalid = "shipping.group.evidence.shipping_type.invalid";

    /// <summary>مبلغ منفی.</summary>
    public const string AmountInvalid = "shipping.group.evidence.amount.invalid";

    /// <summary>ارز نامعتبر.</summary>
    public const string CurrencyInvalid = "shipping.group.evidence.currency.invalid";

    /// <summary>بازهٔ دریافت معکوس/نامعتبر.</summary>
    public const string ReceivingIntervalInvalid = "shipping.group.evidence.receiving_interval.invalid";

    /// <summary>منطقهٔ زمانی نامعتبر.</summary>
    public const string TimeZoneInvalid = "shipping.group.evidence.time_zone.invalid";

    /// <summary>روزهای تحویل ناقص/معکوس.</summary>
    public const string DeliveryDaysInvalid = "shipping.group.evidence.delivery_days.invalid";

    /// <summary>شاهد با ارز گروه نمی‌خواند.</summary>
    public const string CurrencyMismatch = "shipping.group.evidence.currency.mismatch";

    /// <summary>شاهد با مبلغ قابل‌پرداخت هنگام تسویهٔ گروه نمی‌خواند.</summary>
    public const string PayableMismatch = "shipping.group.evidence.payable.mismatch";
}
