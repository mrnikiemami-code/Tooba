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
    /// تعداد صحیح.
    /// </summary>
    public int Quantity { get; init; }

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
    public Guid? ReservationId { get; init; }

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
    /// خط را از نقل‌قول تازه، تخفیف ارزیابی‌شده و نتیجهٔ مالیات می‌سازد.
    /// </summary>
    public static OrderLine FromCheckout(
        Guid sellerOrderId,
        Guid offerId,
        Guid catalogVariantId,
        Guid sellerPartyId,
        int quantity,
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
        DateTimeOffset? promotionAppliedAt = null)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("تعداد خط سفارش باید مثبت باشد.");
        }

        if (!taxExclusive)
        {
            throw new InvalidOperationException("قیمت پایه باید بدون مالیات باشد؛ Tax مبلغ را داخل Pricing دفن نمی‌کند.");
        }

        return new OrderLine
        {
            LineId = UuidV7.New(),
            SellerOrderId = sellerOrderId,
            OfferId = offerId,
            CatalogVariantId = catalogVariantId,
            SellerPartyId = sellerPartyId,
            Quantity = quantity,
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
        };
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
    /// لغو سفارش فروشنده.
    /// </summary>
    public void Cancel()
    {
        if (Status == SellerOrderStatus.Cancelled)
        {
            return;
        }

        Status = SellerOrderStatus.Cancelled;
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
        DateTimeOffset now)
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
