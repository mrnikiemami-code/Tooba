using Tooba.BuildingBlocks;
using Tooba.Offer.Domain;

namespace Tooba.Order.Domain;

/// <summary>
/// حالت سفارش. از وضعیت پرداخت استنتاج نمی‌شود و سبد نیست.
/// </summary>
public enum OrderMode
{
    /// <summary>
    /// درخواست رزرو؛ پرداخت الزامی نیست و خرید آنلاین پرداخت‌نشده نیست.
    /// </summary>
    RequestToReserve = 0,

    /// <summary>
    /// خرید آنلاین؛ چرخهٔ پرداخت جدا است و اینجا Paid ثبت نمی‌شود.
    /// </summary>
    OnlinePurchase = 1,
}

/// <summary>
/// وضعیت سفارش یک فروشنده. پرداخت و ارسال حقیقت جدا هستند.
/// </summary>
public enum SellerOrderStatus
{
    /// <summary>
    /// ثبت شده.
    /// </summary>
    Submitted = 0,

    /// <summary>
    /// خرید آنلاین در انتظار پرداخت آینده؛ Paid نیست.
    /// </summary>
    PendingPayment = 1,

    /// <summary>
    /// درخواست رزرو ثبت شده؛ پذیرش فروشنده آینده است.
    /// </summary>
    ReservationRequested = 2,

    /// <summary>
    /// لغو شده.
    /// </summary>
    Cancelled = 3,

    /// <summary>
    /// پرداخت تأییدشده از ماژول Payment؛ شروع درگاه این وضعیت را نمی‌سازد.
    /// </summary>
    Paid = 4,
}

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
        decimal? quantityStepSnapshot = null)
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

/// <summary>
/// سفارش یک فروشنده داخل checkout. چرخهٔ ارسال جدا است.
/// </summary>
public sealed class SellerOrder
{
    /// <summary>
    /// خطوط این فروشنده برای پایداری EF. Navigation کاتالوگ نیست.
    /// </summary>
    public List<OrderLine> Lines { get; } = [];

    /// <summary>
    /// سازندهٔ EF.
    /// </summary>
    private SellerOrder()
    {
    }

    /// <summary>
    /// شناسهٔ داخلی سفارش فروشنده.
    /// </summary>
    public Guid SellerOrderId { get; init; }

    /// <summary>
    /// گروه checkout والد.
    /// </summary>
    public Guid CheckoutId { get; init; }

    /// <summary>
    /// شمارهٔ مرجع قابل‌نمایش؛ مجوز دسترسی نیست.
    /// </summary>
    public string OrderNumber { get; init; } = string.Empty;

    /// <summary>
    /// فروشندهٔ این سفارش.
    /// </summary>
    public Guid SellerPartyId { get; init; }

    /// <summary>
    /// وضعیت این فروشنده، نه کل سبد.
    /// </summary>
    public SellerOrderStatus Status { get; private set; }

    /// <summary>
    /// وضعیت قبل از لغو؛ برای بازگردانی ایمن لازم است.
    /// </summary>
    public SellerOrderStatus? CancelledFromStatus { get; private set; }

    /// <summary>
    /// زمان آخرین بازگردانی از لغو.
    /// </summary>
    public DateTimeOffset? LastRestoredAt { get; private set; }

    /// <summary>
    /// جمع تصویر خطوط.
    /// </summary>
    public decimal SubtotalSnapshot { get; private set; }

    /// <summary>
    /// جمع مالیات تصویر خطوط در لحظهٔ checkout. قاعدهٔ بعدی این عدد را عوض نمی‌کند.
    /// </summary>
    public decimal TaxSnapshot { get; private set; }

    /// <summary>
    /// جمع تخفیف تصویر خطوط در لحظهٔ checkout. پروموشن بعدی این عدد را عوض نمی‌کند.
    /// </summary>
    public decimal DiscountSnapshot { get; private set; }

    /// <summary>
    /// جمع نهایی تصویر.
    /// </summary>
    public decimal GrandTotalSnapshot { get; private set; }

    /// <summary>
    /// ارز تصویر.
    /// </summary>
    public string Currency { get; init; } = string.Empty;

    /// <summary>
    /// سفارش فروشنده را می‌سازد.
    /// </summary>
    public static SellerOrder Open(
        Guid checkoutId,
        Guid sellerPartyId,
        string orderNumber,
        OrderMode mode,
        string currency,
        IReadOnlyList<OrderLine> lines)
    {
        if (lines.Count == 0)
        {
            throw new InvalidOperationException("سفارش فروشنده بدون خط ساخته نمی‌شود.");
        }

        var order = new SellerOrder
        {
            SellerOrderId = lines[0].SellerOrderId,
            CheckoutId = checkoutId,
            SellerPartyId = sellerPartyId,
            OrderNumber = orderNumber,
            Currency = currency,
            Status = mode == OrderMode.OnlinePurchase
                ? SellerOrderStatus.PendingPayment
                : SellerOrderStatus.ReservationRequested,
        };
        foreach (var line in lines)
        {
            order.Lines.Add(line);
        }

        order.SubtotalSnapshot = lines.Sum(x => x.LineTotalSnapshot);
        order.TaxSnapshot = lines.Sum(x => x.TaxAmountSnapshot);
        order.DiscountSnapshot = lines.Sum(x => x.DiscountAmountSnapshot);
        order.GrandTotalSnapshot = order.SubtotalSnapshot - order.DiscountSnapshot + order.TaxSnapshot;
        return order;
    }

    /// <summary>
    /// لغو سفارش باز (قبل از Paid). مسیر authoritative؛ caller نباید فقط به UI تکیه کند.
    /// </summary>
    public void Cancel()
    {
        if (Status == SellerOrderStatus.Cancelled)
        {
            return;
        }

        if (Status is SellerOrderStatus.PendingPayment
            or SellerOrderStatus.Submitted
            or SellerOrderStatus.ReservationRequested)
        {
            CancelledFromStatus = Status;
            Status = SellerOrderStatus.Cancelled;
            return;
        }

        throw new InvalidOperationException(
            "order.cancel.forbidden: لغو از این وضعیت سفارش مجاز نیست.");
    }

    /// <summary>
    /// لغو سفارش Paid فقط وقتی application ثابت کرده هنوز محموله/ارسال مسدودکننده ندارد.
    /// </summary>
    public void CancelPaidBeforeShipment()
    {
        if (Status == SellerOrderStatus.Cancelled)
        {
            return;
        }

        if (Status != SellerOrderStatus.Paid)
        {
            throw new InvalidOperationException(
                "order.cancel.forbidden: لغو پیش از ارسال فقط برای سفارش Paid مجاز است.");
        }

        CancelledFromStatus = Status;
        Status = SellerOrderStatus.Cancelled;
    }

    /// <summary>
    /// سفارش لغوشده را به وضعیت ذخیره‌شدهٔ قبل از لغو برمی‌گرداند.
    /// </summary>
    public void RestoreFromCancellation(DateTimeOffset at)
    {
        if (Status != SellerOrderStatus.Cancelled)
        {
            throw new InvalidOperationException("order.restore.invalid_state");
        }

        if (CancelledFromStatus is null)
        {
            throw new InvalidOperationException("order.restore.missing_snapshot");
        }

        Status = CancelledFromStatus.Value;
        CancelledFromStatus = null;
        LastRestoredAt = at;
    }

    /// <summary>
    /// پرداخت تأییدشده را روی سفارش خرید آنلاین ثبت می‌کند. متن callback این متد را صدا نمی‌زند.
    /// </summary>
    public void RecordVerifiedPayment()
    {
        if (Status == SellerOrderStatus.Paid)
        {
            return;
        }

        if (Status != SellerOrderStatus.PendingPayment)
        {
            throw new InvalidOperationException("فقط سفارش در انتظار پرداخت پس از Verify درگاه Paid می‌شود.");
        }

        Status = SellerOrderStatus.Paid;
    }

    /// <summary>
    /// برگشت Paid به انتظار پرداخت وقتی تأیید واریز دستی لغو می‌شود.
    /// </summary>
    public void RevertVerifiedPayment()
    {
        if (Status == SellerOrderStatus.PendingPayment)
        {
            return;
        }

        if (Status != SellerOrderStatus.Paid)
        {
            throw new InvalidOperationException("order.payment.unconfirm.invalid_state");
        }

        Status = SellerOrderStatus.PendingPayment;
    }
}

/// <summary>
/// گروه checkout مشتری. سبد نیست و پرداخت نیست.
/// </summary>
public sealed class CheckoutGroup : IHasDomainEvents
{
    private readonly DomainEventCollector _domainEvents = new();

    /// <summary>
    /// سازندهٔ EF.
    /// </summary>
    private CheckoutGroup()
    {
    }

    /// <summary>
    /// شناسهٔ checkout؛ مرجع مشترک چند سفارش فروشنده.
    /// </summary>
    public Guid CheckoutId { get; init; }

    /// <summary>
    /// کلید تکرارناپذیری ارسال.
    /// </summary>
    public string IdempotencyKey { get; init; } = string.Empty;

    /// <summary>
    /// سبد مبدأ.
    /// </summary>
    public Guid CartId { get; init; }

    /// <summary>
    /// حالت سفارش.
    /// </summary>
    public OrderMode Mode { get; init; }

    /// <summary>
    /// طرف اقتصادی خریدار؛ با کاربر عامل یکی نیست.
    /// </summary>
    public Guid? BuyerPartyId { get; init; }

    /// <summary>
    /// کاربر عامل ثبت؛ هویت فروشنده نیست.
    /// </summary>
    public Guid PlacedByUserId { get; init; }

    /// <summary>
    /// بازار.
    /// </summary>
    public string Market { get; init; } = string.Empty;

    /// <summary>
    /// ارز.
    /// </summary>
    public string Currency { get; init; } = string.Empty;

    /// <summary>
    /// کانال.
    /// </summary>
    public SalesChannel Channel { get; init; }

    /// <summary>
    /// ایجاد UTC.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// ارسال UTC.
    /// </summary>
    public DateTimeOffset SubmittedAt { get; init; }

    /// <summary>
    /// نام گیرنده در تصویر ارسال checkout. دفترچهٔ آدرس مشتری نیست.
    /// </summary>
    public string RecipientName { get; init; } = string.Empty;

    /// <summary>
    /// تماس گیرنده در تصویر checkout.
    /// </summary>
    public string ContactMobile { get; init; } = string.Empty;

    /// <summary>
    /// استان تصویر ارسال.
    /// </summary>
    public string ProvinceName { get; init; } = string.Empty;

    /// <summary>
    /// شهر تصویر ارسال.
    /// </summary>
    public string CityName { get; init; } = string.Empty;

    /// <summary>
    /// نشانی پستی تصویر ارسال.
    /// </summary>
    public string PostalAddress { get; init; } = string.Empty;

    /// <summary>
    /// کد پستی تصویر ارسال.
    /// </summary>
    public string PostalCode { get; init; } = string.Empty;

    /// <summary>
    /// کد روش ارسال اولیه؛ موتور حمل‌ونقل جدا نیست.
    /// </summary>
    public string ShippingMethodCode { get; init; } = string.Empty;

    /// <summary>
    /// برچسب روش ارسال اولیه.
    /// </summary>
    public string ShippingMethodLabel { get; init; } = string.Empty;

    /// <summary>
    /// سفارش‌های فروشندهٔ این checkout. یک فروشنده کل checkout را مالک نمی‌شود.
    /// </summary>
    public List<SellerOrder> SellerOrders { get; } = [];

    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.Events;

    /// <inheritdoc />
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// checkout را پس از اعتبارسنجی باز می‌کند.
    /// </summary>
    public static CheckoutGroup Submit(
        Guid checkoutId,
        string idempotencyKey,
        Guid cartId,
        OrderMode mode,
        Guid? buyerPartyId,
        Guid placedByUserId,
        string market,
        string currency,
        SalesChannel channel,
        IReadOnlyList<SellerOrder> sellerOrders,
        DateTimeOffset now,
        string recipientName = "",
        string contactMobile = "",
        string provinceName = "",
        string cityName = "",
        string postalAddress = "",
        string postalCode = "",
        string shippingMethodCode = "",
        string shippingMethodLabel = "")
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new InvalidOperationException("کلید idempotency checkout اجباری است.");
        }

        if (placedByUserId == Guid.Empty)
        {
            throw new InvalidOperationException("کاربر عامل ثبت باید مشخص باشد؛ checkout مهمان کامل در این foundation به تعویق است.");
        }

        if (sellerOrders.Count == 0)
        {
            throw new InvalidOperationException("checkout بدون سفارش فروشنده نیست.");
        }

        var group = new CheckoutGroup
        {
            CheckoutId = checkoutId,
            IdempotencyKey = idempotencyKey.Trim(),
            CartId = cartId,
            Mode = mode,
            BuyerPartyId = buyerPartyId,
            PlacedByUserId = placedByUserId,
            Market = market,
            Currency = currency,
            Channel = channel,
            CreatedAt = now,
            SubmittedAt = now,
            RecipientName = recipientName.Trim(),
            ContactMobile = contactMobile.Trim(),
            ProvinceName = provinceName.Trim(),
            CityName = cityName.Trim(),
            PostalAddress = postalAddress.Trim(),
            PostalCode = postalCode.Trim(),
            ShippingMethodCode = shippingMethodCode.Trim(),
            ShippingMethodLabel = shippingMethodLabel.Trim(),
        };
        foreach (var order in sellerOrders)
        {
            group.SellerOrders.Add(order);
        }

        group._domainEvents.Add(new CheckoutSubmittedDomainEvent(checkoutId, cartId, mode));
        foreach (var order in sellerOrders)
        {
            group._domainEvents.Add(new SellerOrderCreatedDomainEvent(checkoutId, order.SellerOrderId, order.SellerPartyId, mode));
        }

        return group;
    }

    /// <summary>
    /// آیا هویت حق دیدن checkout را دارد. شمارهٔ سفارش به‌تنهایی کافی نیست.
    /// </summary>
    public bool CanBeViewedBy(Guid? buyerPartyId, Guid? placedByUserId)
    {
        if (placedByUserId is not null && placedByUserId == PlacedByUserId)
        {
            return true;
        }

        return buyerPartyId is not null && BuyerPartyId is not null && buyerPartyId == BuyerPartyId;
    }
}

/// <summary>
/// رویداد ارسال checkout.
/// </summary>
public sealed class CheckoutSubmittedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد را می‌سازد.
    /// </summary>
    public CheckoutSubmittedDomainEvent(Guid checkoutId, Guid cartId, OrderMode mode)
    {
        CheckoutId = checkoutId;
        CartId = cartId;
        Mode = mode;
        Metadata = EventMetadataFactory.ForDomain("order.checkout_submitted.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>
    /// checkout.
    /// </summary>
    public Guid CheckoutId { get; }

    /// <summary>
    /// سبد مبدأ.
    /// </summary>
    public Guid CartId { get; }

    /// <summary>
    /// حالت.
    /// </summary>
    public OrderMode Mode { get; }
}

/// <summary>
/// رویداد ایجاد سفارش فروشنده.
/// </summary>
public sealed class SellerOrderCreatedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد را می‌سازد.
    /// </summary>
    public SellerOrderCreatedDomainEvent(Guid checkoutId, Guid sellerOrderId, Guid sellerPartyId, OrderMode mode)
    {
        CheckoutId = checkoutId;
        SellerOrderId = sellerOrderId;
        SellerPartyId = sellerPartyId;
        Mode = mode;
        Metadata = EventMetadataFactory.ForDomain("order.seller_order_created.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>
    /// checkout.
    /// </summary>
    public Guid CheckoutId { get; }

    /// <summary>
    /// سفارش فروشنده.
    /// </summary>
    public Guid SellerOrderId { get; }

    /// <summary>
    /// فروشنده.
    /// </summary>
    public Guid SellerPartyId { get; }

    /// <summary>
    /// حالت.
    /// </summary>
    public OrderMode Mode { get; }
}

/// <summary>
/// یادداشت عملیاتی داخلی روی checkout. فقط برای اپراتور؛ soft-delete مجاز طبق قاعدهٔ مشاهده.
/// </summary>
public sealed class CheckoutOperationalNote
{
    /// <summary>سازندهٔ EF.</summary>
    private CheckoutOperationalNote()
    {
    }

    /// <summary>شناسهٔ یادداشت.</summary>
    public Guid NoteId { get; init; }

    /// <summary>checkout مالک.</summary>
    public Guid CheckoutId { get; init; }

    /// <summary>متن یادداشت (حداکثر ۲۰۰۰ نویسه).</summary>
    public string Body { get; private set; } = string.Empty;

    /// <summary>کاربر ثبت‌کننده.</summary>
    public Guid CreatedByUserId { get; init; }

    /// <summary>زمان ثبت UTC.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>زمان soft-delete؛ تهی یعنی فعال.</summary>
    public DateTimeOffset? DeletedAt { get; private set; }

    /// <summary>کاربر حذف‌کننده.</summary>
    public Guid? DeletedByUserId { get; private set; }

    /// <summary>حداکثر طول مجاز متن.</summary>
    public const int MaxBodyLength = 2000;

    /// <summary>یادداشت می‌سازد.</summary>
    public static CheckoutOperationalNote Create(
        Guid checkoutId,
        Guid createdByUserId,
        string body,
        DateTimeOffset now)
    {
        if (checkoutId == Guid.Empty)
        {
            throw new InvalidOperationException("شناسهٔ checkout نامعتبر است.");
        }

        if (createdByUserId == Guid.Empty)
        {
            throw new InvalidOperationException("شناسهٔ کاربر ثبت‌کننده نامعتبر است.");
        }

        var trimmed = (body ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            throw new InvalidOperationException("متن یادداشت خالی است.");
        }

        if (trimmed.Length > MaxBodyLength)
        {
            throw new InvalidOperationException($"متن یادداشت حداکثر {MaxBodyLength} نویسه است.");
        }

        return new CheckoutOperationalNote
        {
            NoteId = UuidV7.New(),
            CheckoutId = checkoutId,
            Body = trimmed,
            CreatedByUserId = createdByUserId,
            CreatedAt = now,
        };
    }

    /// <summary>soft-delete توسط نویسنده وقتی قفل نشده باشد.</summary>
    public void SoftDelete(Guid actorUserId, DateTimeOffset now)
    {
        if (DeletedAt is not null)
        {
            throw new InvalidOperationException("یادداشت قبلاً حذف شده است.");
        }

        if (actorUserId != CreatedByUserId)
        {
            throw new InvalidOperationException("فقط نویسنده می‌تواند یادداشت را حذف کند.");
        }

        DeletedAt = now;
        DeletedByUserId = actorUserId;
    }
}

/// <summary>
/// ثبت مشاهدهٔ Admin برای قفل حذف یادداشت پس از مشاهدهٔ کاربر دیگر.
/// </summary>
public sealed class CheckoutAdminViewAck
{
    /// <summary>سازندهٔ EF.</summary>
    private CheckoutAdminViewAck()
    {
    }

    /// <summary>شناسهٔ ack.</summary>
    public Guid AckId { get; init; }

    /// <summary>checkout مشاهده‌شده.</summary>
    public Guid CheckoutId { get; init; }

    /// <summary>مشاهده‌کننده.</summary>
    public Guid ViewerUserId { get; init; }

    /// <summary>زمان مشاهده.</summary>
    public DateTimeOffset ViewedAt { get; init; }

    /// <summary>ack جدید.</summary>
    public static CheckoutAdminViewAck Create(Guid checkoutId, Guid viewerUserId, DateTimeOffset now)
    {
        if (checkoutId == Guid.Empty || viewerUserId == Guid.Empty)
        {
            throw new InvalidOperationException("شناسهٔ مشاهده نامعتبر است.");
        }

        return new CheckoutAdminViewAck
        {
            AckId = UuidV7.New(),
            CheckoutId = checkoutId,
            ViewerUserId = viewerUserId,
            ViewedAt = now,
        };
    }
}
