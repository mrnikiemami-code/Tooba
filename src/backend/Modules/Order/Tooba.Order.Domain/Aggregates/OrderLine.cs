using Tooba.BuildingBlocks;
using Tooba.Order.Domain.Enums;

namespace Tooba.Order.Domain.Aggregates;

/// <summary>
/// خط سفارش با تصویر قیمت تاریخی. حقیقت جاری Pricing نیست.
/// </summary>
public sealed class OrderLine
{
    /// <summary>
    /// سازندهٔ EF.
    /// </summary>
    private OrderLine()
    {
    }

    /// <summary>
    /// شناسهٔ خط سفارش.
    /// </summary>
    public Guid LineId { get; init; }

    /// <summary>
    /// سفارش فروشندهٔ مالک.
    /// </summary>
    public Guid SellerOrderId { get; init; }

    /// <summary>
    /// Offer مبدأ خط سبد.
    /// </summary>
    public Guid OfferId { get; init; }

    /// <summary>
    /// گونهٔ کاتالوگ کپی‌شده؛ FK کاتالوگ نیست.
    /// </summary>
    public Guid CatalogVariantId { get; init; }

    /// <summary>
    /// فروشنده.
    /// </summary>
    public Guid SellerPartyId { get; init; }

    /// <summary>
    /// تعداد کالای خط؛ اعشاری مجاز.
    /// </summary>
    public decimal Quantity { get; init; }

    /// <summary>شناسه واحد در لحظهٔ checkout.</summary>
    public Guid? UnitOfMeasureIdSnapshot { get; init; }

    /// <summary>کد واحد تاریخی.</summary>
    public string? UnitCodeSnapshot { get; init; }

    /// <summary>برچسب واحد تاریخی.</summary>
    public string? UnitDisplaySnapshot { get; init; }

    /// <summary>رقم اعشار مؤثر در لحظهٔ checkout.</summary>
    public int QuantityDecimalPlacesSnapshot { get; init; }

    /// <summary>گام مقدار تاریخی.</summary>
    public decimal? QuantityStepSnapshot { get; init; }

    /// <summary>
    /// مبلغ واحد در لحظهٔ تأیید checkout.
    /// </summary>
    public decimal UnitPriceSnapshot { get; init; }

    /// <summary>
    /// جمع خط در لحظهٔ تأیید.
    /// </summary>
    public decimal LineTotalSnapshot { get; init; }

    /// <summary>
    /// ارز تصویر؛ Locale نیست.
    /// </summary>
    public string Currency { get; init; } = string.Empty;

    /// <summary>
    /// مبلغ پایه بدون مالیات طبق قرارداد Pricing.
    /// </summary>
    public bool TaxExclusive { get; init; }

    /// <summary>
    /// شناسهٔ قیمت انتخاب‌شده در تأیید.
    /// </summary>
    public Guid PriceId { get; init; }

    /// <summary>
    /// رزرو موجودی منتقل‌شده از سبد؛ جدول Inventory اینجا نیست.
    /// </summary>
    public Guid? ReservationId { get; private set; }

    /// <summary>
    /// نتیجهٔ مالیات در لحظهٔ checkout. از قاعدهٔ بعدی بازمحاسبه نمی‌شود.
    /// </summary>
    public string TaxOutcomeSnapshot { get; init; } = string.Empty;

    /// <summary>
    /// نرخ اعمال‌شده در تصویر تاریخی.
    /// </summary>
    public decimal TaxRateSnapshot { get; init; }

    /// <summary>
    /// مبلغ مالیات خط در تصویر تاریخی.
    /// </summary>
    public decimal TaxAmountSnapshot { get; init; }

    /// <summary>عوارض خط؛ جدا از مالیات.</summary>
    public decimal DutyAmountSnapshot { get; init; }

    /// <summary>
    /// مبلغ با مالیات خط در تصویر تاریخی.
    /// </summary>
    public decimal TaxInclusiveSnapshot { get; init; }

    /// <summary>
    /// قاعدهٔ اعمال‌شده؛ FK به schema tax نیست.
    /// </summary>
    public Guid? TaxRuleIdSnapshot { get; init; }

    /// <summary>
    /// مبلغ تخفیف اعمال‌شده روی خط در لحظهٔ checkout. قیمت تألیف‌شده نیست.
    /// </summary>
    public decimal DiscountAmountSnapshot { get; init; }

    /// <summary>
    /// شناسهٔ پروموشن اعمال‌شده؛ FK به schema promotion نیست.
    /// </summary>
    public Guid? PromotionIdSnapshot { get; init; }

    /// <summary>
    /// نام پروموشن در لحظهٔ اعمال.
    /// </summary>
    public string? PromotionNameSnapshot { get; init; }

    /// <summary>
    /// کد کوپن نرمال‌شده در تصویر تاریخی.
    /// </summary>
    public string? PromotionCodeSnapshot { get; init; }

    /// <summary>
    /// گونهٔ تخفیف در تصویر.
    /// </summary>
    public string? DiscountKindSnapshot { get; init; }

    /// <summary>
    /// مبلغ بدون مالیات قبل از تخفیف.
    /// </summary>
    public decimal PreDiscountTaxExclusiveSnapshot { get; init; }

    /// <summary>
    /// مبلغ بدون مالیات بعد از تخفیف؛ پایهٔ Tax.
    /// </summary>
    public decimal PostDiscountTaxExclusiveSnapshot { get; init; }

    /// <summary>
    /// زمان اعمال پروموشن در تسویه.
    /// </summary>
    public DateTimeOffset? PromotionAppliedAtSnapshot { get; init; }

    /// <summary>
    /// تصویر ردهٔ اصلی گونه در لحظهٔ checkout برای authorization؛ FK به جداول Catalog نیست.
    /// </summary>
    public Guid? CategoryIdSnapshot { get; init; }

    /// <summary>
    /// آیا خط در لحظهٔ خرید قابل مرجوعی بوده است.
    /// </summary>
    public bool IsReturnableSnapshot { get; init; } = true;

    /// <summary>
    /// پنجرهٔ مرجوعی به روز از زمان تحویل — تصویر خرید.
    /// </summary>
    public int ReturnWindowDaysSnapshot { get; init; } = 7;

    /// <summary>
    /// منبع سیاست مرجوعی در لحظهٔ خرید (platform_default / offer_override / non_returnable).
    /// </summary>
    public string? ReturnPolicySourceSnapshot { get; init; }

    /// <summary>
    /// برچسب انسانی سیاست مرجوعی در لحظهٔ خرید.
    /// </summary>
    public string? ReturnPolicyLabelSnapshot { get; init; }

    /// <summary>
    /// خط را از نقل‌قول تازه، تخفیف ارزیابی‌شده و نتیجهٔ مالیات می‌سازد.
    /// </summary>
    public static OrderLine FromCheckout(
        Guid sellerOrderId,
        Guid offerId,
        Guid catalogVariantId,
        Guid sellerPartyId,
        decimal quantity,
        decimal unitPrice,
        string currency,
        bool taxExclusive,
        Guid priceId,
        Guid? reservationId,
        string taxOutcome,
        decimal taxRate,
        decimal taxAmount,
        decimal taxInclusive,
        Guid? taxRuleId,
        decimal discountAmount = 0m,
        Guid? promotionId = null,
        string? promotionName = null,
        string? promotionCode = null,
        string? discountKind = null,
        decimal? preDiscountTaxExclusive = null,
        decimal? postDiscountTaxExclusive = null,
        DateTimeOffset? promotionAppliedAt = null,
        Guid? categoryIdSnapshot = null,
        bool isReturnableSnapshot = true,
        int returnWindowDaysSnapshot = 7,
        string? returnPolicySourceSnapshot = "platform_default",
        string? returnPolicyLabelSnapshot = null,
        Guid? unitOfMeasureIdSnapshot = null,
        string? unitCodeSnapshot = null,
        string? unitDisplaySnapshot = null,
        int quantityDecimalPlacesSnapshot = 0,
        decimal? quantityStepSnapshot = null,
        decimal dutyAmountSnapshot = 0)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("تعداد خط سفارش باید مثبت باشد.");
        }

        if (!taxExclusive)
        {
            throw new InvalidOperationException("قیمت پایه باید بدون مالیات باشد؛ Tax مبلغ را داخل Pricing دفن نمی‌کند.");
        }

        if (returnWindowDaysSnapshot < 0)
        {
            throw new InvalidOperationException("پنجرهٔ مرجوعی نمی‌تواند منفی باشد.");
        }

        var policyLabel = returnPolicyLabelSnapshot
            ?? (isReturnableSnapshot
                ? $"{returnWindowDaysSnapshot} روز پس از تحویل"
                : "غیرقابل مرجوعی");

        return new OrderLine
        {
            LineId = UuidV7.New(),
            SellerOrderId = sellerOrderId,
            OfferId = offerId,
            CatalogVariantId = catalogVariantId,
            SellerPartyId = sellerPartyId,
            Quantity = quantity,
            UnitOfMeasureIdSnapshot = unitOfMeasureIdSnapshot,
            UnitCodeSnapshot = unitCodeSnapshot,
            UnitDisplaySnapshot = unitDisplaySnapshot,
            QuantityDecimalPlacesSnapshot = quantityDecimalPlacesSnapshot,
            QuantityStepSnapshot = quantityStepSnapshot,
            UnitPriceSnapshot = unitPrice,
            LineTotalSnapshot = decimal.Multiply(unitPrice, quantity),
            Currency = currency,
            TaxExclusive = taxExclusive,
            PriceId = priceId,
            ReservationId = reservationId,
            TaxOutcomeSnapshot = taxOutcome,
            TaxRateSnapshot = taxRate,
            TaxAmountSnapshot = taxAmount,
            DutyAmountSnapshot = dutyAmountSnapshot,
            TaxInclusiveSnapshot = taxInclusive,
            TaxRuleIdSnapshot = taxRuleId,
            DiscountAmountSnapshot = discountAmount,
            PromotionIdSnapshot = promotionId,
            PromotionNameSnapshot = promotionName,
            PromotionCodeSnapshot = promotionCode,
            DiscountKindSnapshot = discountKind,
            PreDiscountTaxExclusiveSnapshot = preDiscountTaxExclusive ?? decimal.Multiply(unitPrice, quantity),
            PostDiscountTaxExclusiveSnapshot = postDiscountTaxExclusive ?? decimal.Multiply(unitPrice, quantity) - discountAmount,
            PromotionAppliedAtSnapshot = promotionAppliedAt,
            CategoryIdSnapshot = categoryIdSnapshot,
            IsReturnableSnapshot = isReturnableSnapshot,
            ReturnWindowDaysSnapshot = returnWindowDaysSnapshot,
            ReturnPolicySourceSnapshot = returnPolicySourceSnapshot,
            ReturnPolicyLabelSnapshot = policyLabel,
        };
    }

    /// <summary>
    /// رزرو تازه‌گرفته‌شده را جایگزین رزرو آزادشده می‌کند؛ فقط برای بازگردانی لغو.
    /// </summary>
    public void ReplaceReservation(Guid reservationId)
    {
        if (reservationId == Guid.Empty)
        {
            throw new InvalidOperationException("شناسهٔ رزرو نامعتبر است.");
        }

        ReservationId = reservationId;
    }
}
