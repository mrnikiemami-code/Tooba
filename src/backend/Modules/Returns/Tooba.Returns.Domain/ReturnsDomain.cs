using Tooba.BuildingBlocks;

namespace Tooba.Returns.Domain;

/// <summary>
/// مقصد بازگشت وجه. فقط مقادیر typed؛ free-form نیست.
/// </summary>
public enum RefundDestination
{
    /// <summary>بازگشت به روش پرداخت اصلی (PSP/gateway).</summary>
    OriginalPayment = 0,

    /// <summary>اعتبار به کیف پول مشتری.</summary>
    Wallet = 1,
}

/// <summary>
/// وضعیت درخواست مرجوعی. با وضعیت Order یا Payment یکی نیست.
/// </summary>
public enum ReturnRequestStatus
{
    /// <summary>درخواست ثبت‌شده و در انتظار بررسی.</summary>
    Requested = 0,

    /// <summary>تأیید شده و آمادهٔ refund.</summary>
    Approved = 1,

    /// <summary>رد شده.</summary>
    Rejected = 2,

    /// <summary>refund در حال پردازش.</summary>
    RefundProcessing = 3,

    /// <summary>مرجوعی و refund تکمیل شده.</summary>
    Completed = 4,

    /// <summary>refund شکست خورده.</summary>
    RefundFailed = 5,

    /// <summary>لغو شده توسط مشتری.</summary>
    Cancelled = 6,
}

/// <summary>
/// وضعیت تلاش refund. با PaymentStatus یکی نیست.
/// </summary>
public enum RefundAttemptStatus
{
    /// <summary>در انتظار پاسخ درگاه.</summary>
    Pending = 0,

    /// <summary>موفق.</summary>
    Succeeded = 1,

    /// <summary>شکست.</summary>
    Failed = 2,
}

/// <summary>
/// خط مرجوعی با snapshot قیمت سفارش.
/// </summary>
public sealed class ReturnItem
{
    private ReturnItem()
    {
    }

    /// <summary>شناسه خط مرجوعی.</summary>
    public Guid ReturnItemId { get; init; }

    /// <summary>درخواست مالک.</summary>
    public Guid ReturnRequestId { get; init; }

    /// <summary>خط سفارش مرجع.</summary>
    public Guid OrderLineId { get; init; }

    /// <summary>تعداد درخواستی.</summary>
    public int Quantity { get; init; }

    /// <summary>snapshot قیمت واحد.</summary>
    public decimal UnitPriceSnapshot { get; init; }

    /// <summary>snapshot ارز.</summary>
    public string Currency { get; init; } = string.Empty;

    /// <summary>رزرو موجودی مرجع؛ FK Inventory نیست.</summary>
    public Guid? ReservationId { get; init; }

    internal static ReturnItem Create(
        Guid returnRequestId,
        Guid orderLineId,
        int quantity,
        decimal unitPriceSnapshot,
        string currency,
        Guid? reservationId) =>
        new()
        {
            ReturnItemId = Guid.NewGuid(),
            ReturnRequestId = returnRequestId,
            OrderLineId = orderLineId,
            Quantity = quantity,
            UnitPriceSnapshot = unitPriceSnapshot,
            Currency = currency.Trim(),
            ReservationId = reservationId,
        };
}

/// <summary>
/// تلاش refund برای یک درخواست مرجوعی.
/// </summary>
public sealed class RefundAttempt
{
    private RefundAttempt()
    {
    }

    /// <summary>شناسه تلاش.</summary>
    public Guid RefundAttemptId { get; init; }

    /// <summary>درخواست مرجوعی مالک.</summary>
    public Guid ReturnRequestId { get; init; }

    /// <summary>پرداخت مرجع.</summary>
    public Guid PaymentId { get; init; }

    /// <summary>مبلغ refund.</summary>
    public decimal Amount { get; init; }

    /// <summary>ارز.</summary>
    public string Currency { get; init; } = string.Empty;

    /// <summary>وضعیت تلاش.</summary>
    public RefundAttemptStatus Status { get; private set; }

    /// <summary>کلید idempotency.</summary>
    public string IdempotencyKey { get; init; } = string.Empty;

    /// <summary>مرجع درگاه.</summary>
    public string? ProviderReference { get; private set; }

    /// <summary>کد شکست.</summary>
    public string? FailureCode { get; private set; }

    /// <summary>زمان ایجاد.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>زمان تکمیل.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    internal static RefundAttempt CreatePending(
        Guid returnRequestId,
        Guid paymentId,
        decimal amount,
        string currency,
        string idempotencyKey,
        DateTimeOffset now) =>
        new()
        {
            RefundAttemptId = Guid.NewGuid(),
            ReturnRequestId = returnRequestId,
            PaymentId = paymentId,
            Amount = amount,
            Currency = currency.Trim(),
            Status = RefundAttemptStatus.Pending,
            IdempotencyKey = idempotencyKey.Trim(),
            CreatedAt = now,
        };

    /// <summary>تلاش را موفق علامت می‌زند.</summary>
    public void MarkSucceeded(string providerReference, DateTimeOffset now)
    {
        Status = RefundAttemptStatus.Succeeded;
        ProviderReference = providerReference.Trim();
        CompletedAt = now;
    }

    /// <summary>تلاش را شکست‌خورده علامت می‌زند.</summary>
    public void MarkFailed(string failureCode, DateTimeOffset now)
    {
        Status = RefundAttemptStatus.Failed;
        FailureCode = failureCode.Trim();
        CompletedAt = now;
    }
}

/// <summary>
/// درخواست مرجوعی برای یک SellerOrder. aggregate root.
/// </summary>
public sealed class ReturnRequest : IHasDomainEvents
{
    private readonly DomainEventCollector _domainEvents = new();
    private readonly List<ReturnItem> _items = [];
    private readonly List<RefundAttempt> _refundAttempts = [];

    private ReturnRequest()
    {
    }

    /// <summary>شناسه درخواست.</summary>
    public Guid ReturnRequestId { get; init; }

    /// <summary>سفارش فروشنده مرجع.</summary>
    public Guid SellerOrderId { get; init; }

    /// <summary>checkout مرجع.</summary>
    public Guid CheckoutId { get; init; }

    /// <summary>فروشنده.</summary>
    public Guid SellerPartyId { get; init; }

    /// <summary>کاربر درخواست‌دهنده.</summary>
    public Guid RequestedByUserId { get; init; }

    /// <summary>کلید idempotency ایجاد.</summary>
    public string IdempotencyKey { get; init; } = string.Empty;

    /// <summary>وضعیت چرخه.</summary>
    public ReturnRequestStatus Status { get; private set; }

    /// <summary>دلیل درخواست.</summary>
    public string? Reason { get; init; }

    /// <summary>ارز refund.</summary>
    public string Currency { get; init; } = string.Empty;

    /// <summary>مبلغ refund محاسبه‌شده از snapshot خطوط.</summary>
    public decimal RefundAmount { get; private set; }

    /// <summary>مقصد بازگشت وجه.</summary>
    public RefundDestination RefundDestination { get; private set; }

    /// <summary>شناسه پرداخت snapshot.</summary>
    public Guid? PaymentId { get; private set; }

    /// <summary>زمان ایجاد.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>آخرین به‌روزرسانی.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>خطوط مرجوعی.</summary>
    public IReadOnlyList<ReturnItem> Items => _items;

    /// <summary>تلاش‌های refund.</summary>
    public IReadOnlyList<RefundAttempt> RefundAttempts => _refundAttempts;

    /// <summary>رویدادهای دامنه.</summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.Events;

    /// <inheritdoc />
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// درخواست مرجوعی را از snapshot خطوط می‌سازد. eligibility در Directory بررسی می‌شود.
    /// </summary>
    public static ReturnRequest Create(
        Guid sellerOrderId,
        Guid checkoutId,
        Guid sellerPartyId,
        Guid requestedByUserId,
        string idempotencyKey,
        string? reason,
        string currency,
        IEnumerable<(Guid OrderLineId, int Quantity, decimal UnitPriceSnapshot, Guid? ReservationId)> lines,
        DateTimeOffset now,
        RefundDestination refundDestination = RefundDestination.OriginalPayment)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new InvalidOperationException("کلید idempotency الزامی است.");
        }

        if (!Enum.IsDefined(refundDestination))
        {
            throw new InvalidOperationException("مقصد بازگشت وجه نامعتبر است.");
        }

        var request = new ReturnRequest
        {
            ReturnRequestId = Guid.NewGuid(),
            SellerOrderId = sellerOrderId,
            CheckoutId = checkoutId,
            SellerPartyId = sellerPartyId,
            RequestedByUserId = requestedByUserId,
            IdempotencyKey = idempotencyKey.Trim(),
            Status = ReturnRequestStatus.Requested,
            Reason = reason?.Trim(),
            Currency = currency.Trim(),
            RefundDestination = refundDestination,
            CreatedAt = now,
            UpdatedAt = now,
        };
        foreach (var line in lines)
        {
            if (line.Quantity <= 0)
            {
                throw new InvalidOperationException("تعداد مرجوعی باید مثبت باشد.");
            }

            request._items.Add(ReturnItem.Create(
                request.ReturnRequestId,
                line.OrderLineId,
                line.Quantity,
                line.UnitPriceSnapshot,
                currency,
                line.ReservationId));
        }

        request.RefundAmount = request._items.Sum(x => x.UnitPriceSnapshot * x.Quantity);
        request._domainEvents.Add(new ReturnRequestedDomainEvent(request.ReturnRequestId, request.SellerOrderId, request.CheckoutId));
        return request;
    }

    /// <summary>خطوط بارگذاری‌شده را وصل می‌کند.</summary>
    public void AttachLoadedItems(IEnumerable<ReturnItem> items)
    {
        _items.Clear();
        _items.AddRange(items);
    }

    /// <summary>تلاش‌های بارگذاری‌شده را وصل می‌کند.</summary>
    public void AttachLoadedRefundAttempts(IEnumerable<RefundAttempt> attempts)
    {
        _refundAttempts.Clear();
        _refundAttempts.AddRange(attempts);
    }

    /// <summary>درخواست را تأیید می‌کند.</summary>
    public void Approve(Guid paymentId, DateTimeOffset now, RefundDestination? destinationOverride = null)
    {
        EnsureStatus(ReturnRequestStatus.Requested);
        Status = ReturnRequestStatus.Approved;
        PaymentId = paymentId;
        if (destinationOverride is { } dest)
        {
            if (!Enum.IsDefined(dest))
                throw new InvalidOperationException("مقصد بازگشت وجه نامعتبر است.");
            RefundDestination = dest;
        }

        UpdatedAt = now;
        _domainEvents.Add(new ReturnApprovedDomainEvent(ReturnRequestId, SellerOrderId, CheckoutId, RefundAmount, Currency));
    }

    /// <summary>درخواست را رد می‌کند.</summary>
    public void Reject(DateTimeOffset now)
    {
        EnsureStatus(ReturnRequestStatus.Requested);
        Status = ReturnRequestStatus.Rejected;
        UpdatedAt = now;
    }

    /// <summary>به RefundProcessing می‌رود.</summary>
    public void MarkRefundProcessing(DateTimeOffset now)
    {
        EnsureStatus(ReturnRequestStatus.Approved, ReturnRequestStatus.RefundFailed);
        Status = ReturnRequestStatus.RefundProcessing;
        UpdatedAt = now;
    }

    /// <summary>refund را موفق علامت می‌زند.</summary>
    public void MarkRefundSucceeded(DateTimeOffset now)
    {
        EnsureStatus(ReturnRequestStatus.RefundProcessing);
        Status = ReturnRequestStatus.Completed;
        UpdatedAt = now;
        _domainEvents.Add(new RefundSucceededDomainEvent(ReturnRequestId, SellerOrderId, PaymentId ?? Guid.Empty, RefundAmount, Currency));
    }

    /// <summary>refund را شکست‌خورده علامت می‌زند.</summary>
    public void MarkRefundFailed(DateTimeOffset now)
    {
        EnsureStatus(ReturnRequestStatus.RefundProcessing);
        Status = ReturnRequestStatus.RefundFailed;
        UpdatedAt = now;
    }

    /// <summary>درخواست را لغو می‌کند.</summary>
    public void Cancel(DateTimeOffset now)
    {
        EnsureStatus(ReturnRequestStatus.Requested);
        Status = ReturnRequestStatus.Cancelled;
        UpdatedAt = now;
    }

    /// <summary>تلاش refund جدید ثبت می‌کند.</summary>
    public RefundAttempt BeginRefundAttempt(Guid paymentId, string idempotencyKey, DateTimeOffset now)
    {
        var attempt = RefundAttempt.CreatePending(ReturnRequestId, paymentId, RefundAmount, Currency, idempotencyKey, now);
        _refundAttempts.Add(attempt);
        return attempt;
    }

    private void EnsureStatus(params ReturnRequestStatus[] allowed)
    {
        if (!allowed.Contains(Status))
        {
            throw new InvalidOperationException("انتقال وضعیت از این حالت مجاز نیست.");
        }
    }
}

/// <summary>رویداد درخواست مرجوعی.</summary>
public sealed class ReturnRequestedDomainEvent : IDomainEvent
{
    /// <summary>رویداد را می‌سازد.</summary>
    public ReturnRequestedDomainEvent(Guid returnRequestId, Guid sellerOrderId, Guid checkoutId)
    {
        ReturnRequestId = returnRequestId;
        SellerOrderId = sellerOrderId;
        CheckoutId = checkoutId;
        Metadata = EventMetadataFactory.ForDomain("return.requested.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>شناسه درخواست.</summary>
    public Guid ReturnRequestId { get; }

    /// <summary>سفارش فروشنده.</summary>
    public Guid SellerOrderId { get; }

    /// <summary>checkout مرجع.</summary>
    public Guid CheckoutId { get; }
}

/// <summary>رویداد تأیید مرجوعی.</summary>
public sealed class ReturnApprovedDomainEvent : IDomainEvent
{
    /// <summary>رویداد را می‌سازد.</summary>
    public ReturnApprovedDomainEvent(
        Guid returnRequestId,
        Guid sellerOrderId,
        Guid checkoutId,
        decimal refundAmount,
        string currency)
    {
        ReturnRequestId = returnRequestId;
        SellerOrderId = sellerOrderId;
        CheckoutId = checkoutId;
        RefundAmount = refundAmount;
        Currency = currency;
        Metadata = EventMetadataFactory.ForDomain("return.approved.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>شناسه درخواست.</summary>
    public Guid ReturnRequestId { get; }

    /// <summary>سفارش فروشنده.</summary>
    public Guid SellerOrderId { get; }

    /// <summary>checkout مرجع.</summary>
    public Guid CheckoutId { get; }

    /// <summary>مبلغ refund.</summary>
    public decimal RefundAmount { get; }

    /// <summary>ارز.</summary>
    public string Currency { get; }
}

/// <summary>رویداد موفقیت refund.</summary>
public sealed class RefundSucceededDomainEvent : IDomainEvent
{
    /// <summary>رویداد را می‌سازد.</summary>
    public RefundSucceededDomainEvent(
        Guid returnRequestId,
        Guid sellerOrderId,
        Guid paymentId,
        decimal refundAmount,
        string currency)
    {
        ReturnRequestId = returnRequestId;
        SellerOrderId = sellerOrderId;
        PaymentId = paymentId;
        RefundAmount = refundAmount;
        Currency = currency;
        Metadata = EventMetadataFactory.ForDomain("refund.succeeded.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>شناسه درخواست.</summary>
    public Guid ReturnRequestId { get; }

    /// <summary>سفارش فروشنده.</summary>
    public Guid SellerOrderId { get; }

    /// <summary>پرداخت مرجع.</summary>
    public Guid PaymentId { get; }

    /// <summary>مبلغ refund.</summary>
    public decimal RefundAmount { get; }

    /// <summary>ارز.</summary>
    public string Currency { get; }
}
