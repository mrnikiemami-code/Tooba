using Tooba.BuildingBlocks;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Order.Domain.Enums;
using Tooba.Order.Domain.Events;

namespace Tooba.Order.Domain.Aggregates;

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

    /// <summary>نام کوچک گیرنده در تصویر سفارش؛ رکورد قدیمی ممکن است خالی باشد.</summary>
    public string RecipientFirstName { get; init; } = string.Empty;

    /// <summary>نام خانوادگی گیرنده در تصویر سفارش؛ رکورد قدیمی ممکن است خالی باشد.</summary>
    public string RecipientLastName { get; init; } = string.Empty;

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
    /// مبلغ ارسال نقل‌قول‌شدهٔ backend در لحظهٔ ثبت.
    /// </summary>
    public decimal ShippingAmount { get; init; }

    /// <summary>
    /// حداقل تاریخ تحویل محاسبه‌شدهٔ backend.
    /// </summary>
    public DateOnly? MinimumDeliveryDate { get; init; }

    /// <summary>
    /// تاریخ تحویل انتخابی مشتری (هرگز زودتر از حداقل).
    /// </summary>
    public DateOnly? RequestedDeliveryDate { get; init; }

    /// <summary>
    /// پنجرهٔ ساعتی تحویل انتخابی.
    /// </summary>
    public string RequestedDeliveryTimeWindow { get; init; } = string.Empty;

    /// <summary>
    /// یادداشت مشتری برای ارسال/تحویل (نه یادداشت عملیاتی Admin).
    /// </summary>
    public string CustomerNote { get; init; } = string.Empty;

    /// <summary>
    /// سفارش‌های فروشندهٔ این checkout. یک فروشنده کل checkout را مالک نمی‌شود.
    /// </summary>
    public List<SellerOrder> SellerOrders { get; } = [];

    /// <summary>
    /// گروه‌های ارسال پایدار این checkout (M2). خالی یعنی سفارش قدیمی تک‌روشی که فقط
    /// ستون‌های اسکالر را دارد.
    /// </summary>
    public List<OrderShippingGroup> ShippingGroups { get; } = [];

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
        string shippingMethodLabel = "",
        decimal shippingAmount = 0m,
        DateOnly? minimumDeliveryDate = null,
        DateOnly? requestedDeliveryDate = null,
        string requestedDeliveryTimeWindow = "",
        string customerNote = "",
        string recipientFirstName = "",
        string recipientLastName = "",
        IReadOnlyList<OrderShippingGroup>? shippingGroups = null)
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

        var acceptedGroups = shippingGroups ?? [];
        // The payable is the sum of committed group charges, counted exactly once.
        var payableShippingAmount = acceptedGroups.Count > 0
            ? OrderShippingGroupMaterializer.SumShippingCharge(acceptedGroups)
            : Math.Max(0m, shippingAmount);
        var multiGroup = acceptedGroups.Count > 1;

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
            RecipientFirstName = recipientFirstName.Trim(),
            RecipientLastName = recipientLastName.Trim(),
            ContactMobile = contactMobile.Trim(),
            ProvinceName = provinceName.Trim(),
            CityName = cityName.Trim(),
            PostalAddress = postalAddress.Trim(),
            PostalCode = postalCode.Trim(),
            ShippingMethodCode = multiGroup ? string.Empty : shippingMethodCode.Trim(),
            ShippingMethodLabel = multiGroup ? string.Empty : shippingMethodLabel.Trim(),
            ShippingAmount = payableShippingAmount,
            MinimumDeliveryDate = minimumDeliveryDate,
            RequestedDeliveryDate = requestedDeliveryDate,
            RequestedDeliveryTimeWindow = string.IsNullOrWhiteSpace(requestedDeliveryTimeWindow)
                ? string.Empty
                : requestedDeliveryTimeWindow.Trim(),
            CustomerNote = string.IsNullOrWhiteSpace(customerNote) ? string.Empty : customerNote.Trim(),
        };
        foreach (var order in sellerOrders)
        {
            group.SellerOrders.Add(order);
        }

        // M2: accepted shipping groups are materialized once at this boundary and never rewritten.
        foreach (var shippingGroup in acceptedGroups)
        {
            group.ShippingGroups.Add(shippingGroup);
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

