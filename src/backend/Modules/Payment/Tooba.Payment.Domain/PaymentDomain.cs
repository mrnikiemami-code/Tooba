using Tooba.BuildingBlocks;

namespace Tooba.Payment.Domain;

/// <summary>
/// وضعیت پرداخت. با وضعیت سفارش یکی نیست؛ شروع درگاه به‌معنای Succeeded نیست.
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    /// رکورد ساخته شده و هنوز به درگاه نرفته.
    /// </summary>
    Created = 0,

    /// <summary>
    /// شروع درگاه انجام شده؛ تا تأیید مستقل Succeeded نیست.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// فقط پس از Verify موفق درگاه.
    /// </summary>
    Succeeded = 2,

    /// <summary>
    /// تأیید یا تلاش شکست خورد.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// لغو شده؛ سفارش را خودکار Paid نمی‌کند.
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// مهلت تلاش تمام شده.
    /// </summary>
    Expired = 5,
}

/// <summary>
/// وضعیت یک تلاش درگاه. تاریخچهٔ تلاش بازنویسی نمی‌شود.
/// </summary>
public enum PaymentAttemptStatus
{
    /// <summary>
    /// شروع شده.
    /// </summary>
    Initiated = 0,

    /// <summary>
    /// درگاه Verify را تأیید کرد.
    /// </summary>
    VerifiedSucceeded = 1,

    /// <summary>
    /// درگاه Verify را رد کرد یا شکست اعلام کرد.
    /// </summary>
    VerifiedFailed = 2,

    /// <summary>
    /// تلاش لغو شد.
    /// </summary>
    Cancelled = 3,
}

/// <summary>
/// تخصیص مبلغ یک پرداخت مشتری روی سفارش فروشنده. تسویه/پayout نیست.
/// </summary>
public sealed class PaymentAllocation
{
    /// <summary>
    /// سازندهٔ EF.
    /// </summary>
    private PaymentAllocation()
    {
    }

    /// <summary>
    /// شناسهٔ تخصیص.
    /// </summary>
    public Guid AllocationId { get; init; }

    /// <summary>
    /// پرداخت مالک.
    /// </summary>
    public Guid PaymentId { get; init; }

    /// <summary>
    /// سفارش فروشنده؛ FK به schema سفارش نیست.
    /// </summary>
    public Guid SellerOrderId { get; init; }

    /// <summary>
    /// مبلغ تخصیص‌یافته.
    /// </summary>
    public decimal AllocatedAmount { get; init; }

    /// <summary>
    /// ارز تخصیص؛ باید با پرداخت یکی باشد.
    /// </summary>
    public string Currency { get; init; } = string.Empty;

    /// <summary>
    /// تخصیص را می‌سازد.
    /// </summary>
    public static PaymentAllocation Create(Guid paymentId, Guid sellerOrderId, decimal amount, string currency)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("تخصیص پرداخت باید مبلغ مثبت داشته باشد.");
        }

        return new PaymentAllocation
        {
            AllocationId = Guid.NewGuid(),
            PaymentId = paymentId,
            SellerOrderId = sellerOrderId,
            AllocatedAmount = amount,
            Currency = currency,
        };
    }
}

/// <summary>
/// یک تلاش درگاه. شناسهٔ داخلی با شمارهٔ تراکنش ارائه‌دهنده یکی نیست.
/// </summary>
public sealed class PaymentAttempt
{
    /// <summary>
    /// سازندهٔ EF.
    /// </summary>
    private PaymentAttempt()
    {
    }

    /// <summary>
    /// شناسهٔ تلاش.
    /// </summary>
    public Guid AttemptId { get; init; }

    /// <summary>
    /// پرداخت مالک.
    /// </summary>
    public Guid PaymentId { get; init; }

    /// <summary>
    /// کد درگاه انتزاعی.
    /// </summary>
    public string ProviderCode { get; init; } = string.Empty;

    /// <summary>
    /// مرجع درخواست نزد درگاه.
    /// </summary>
    public string ProviderRequestReference { get; private set; } = string.Empty;

    /// <summary>
    /// مرجع تراکنش پس از تأیید؛ کلید یکتایی درگاه است نه PK داخلی.
    /// </summary>
    public string? ProviderTransactionReference { get; private set; }

    /// <summary>
    /// وضعیت تلاش.
    /// </summary>
    public PaymentAttemptStatus Status { get; private set; }

    /// <summary>
    /// زمان ایجاد.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// زمان پایان تلاش.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>
    /// کد شکست درگاه در صورت وجود.
    /// </summary>
    public string? FailureCode { get; private set; }

    /// <summary>
    /// تلاش را پس از شروع درگاه می‌سازد.
    /// </summary>
    public static PaymentAttempt Initiate(Guid paymentId, string providerCode, string requestReference, DateTimeOffset at)
    {
        return new PaymentAttempt
        {
            AttemptId = Guid.NewGuid(),
            PaymentId = paymentId,
            ProviderCode = providerCode,
            ProviderRequestReference = requestReference,
            Status = PaymentAttemptStatus.Initiated,
            CreatedAt = at,
        };
    }

    /// <summary>
    /// تأیید موفق درگاه را روی همین تلاش ثبت می‌کند؛ تلاش قبلی را پاک نمی‌کند.
    /// </summary>
    public void MarkVerifiedSuccess(string transactionReference, DateTimeOffset at)
    {
        if (Status != PaymentAttemptStatus.Initiated)
        {
            return;
        }

        ProviderTransactionReference = transactionReference;
        Status = PaymentAttemptStatus.VerifiedSucceeded;
        CompletedAt = at;
    }

    /// <summary>
    /// تأیید ناموفق درگاه را ثبت می‌کند.
    /// </summary>
    public void MarkVerifiedFailure(string? failureCode, DateTimeOffset at)
    {
        if (Status != PaymentAttemptStatus.Initiated)
        {
            return;
        }

        FailureCode = failureCode;
        Status = PaymentAttemptStatus.VerifiedFailed;
        CompletedAt = at;
    }
}

/// <summary>
/// پرداخت مشتری روی تصویر تجاری سفارش. کارت ذخیره نمی‌شود و متن callback حقیقت نیست.
/// </summary>
public sealed class CustomerPayment : IHasDomainEvents
{
    private readonly DomainEventCollector _domainEvents = new();
    private readonly List<PaymentAttempt> _attempts = [];
    private readonly List<PaymentAllocation> _allocations = [];

    /// <summary>
    /// سازندهٔ EF.
    /// </summary>
    private CustomerPayment()
    {
    }

    /// <summary>
    /// شناسهٔ داخلی پرداخت؛ شمارهٔ درگاه نیست.
    /// </summary>
    public Guid PaymentId { get; init; }

    /// <summary>
    /// گروه checkout مرجع؛ FK دیتابیس سفارش نیست.
    /// </summary>
    public Guid CheckoutId { get; init; }

    /// <summary>
    /// مبلغ از تصویر سفارش؛ مشتری تعیین نمی‌کند.
    /// </summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// ارز تصویر سفارش.
    /// </summary>
    public string Currency { get; init; } = string.Empty;

    /// <summary>
    /// وضعیت پرداخت.
    /// </summary>
    public PaymentStatus Status { get; private set; }

    /// <summary>
    /// کد درگاه انتخاب‌شده.
    /// </summary>
    public string ProviderCode { get; init; } = string.Empty;

    /// <summary>
    /// کلید تکرار شروع.
    /// </summary>
    public string IdempotencyKey { get; init; } = string.Empty;

    /// <summary>
    /// زمان ایجاد.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// زمان به‌روزرسانی.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// زمان موفقیت تأییدشده.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>
    /// تلاش‌ها.
    /// </summary>
    public IReadOnlyCollection<PaymentAttempt> Attempts => _attempts;

    /// <summary>
    /// تخصیص چندفروشنده.
    /// </summary>
    public IReadOnlyCollection<PaymentAllocation> Allocations => _allocations;

    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.Events;

    /// <inheritdoc />
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// پرداخت را از تصویر سفارش می‌سازد. مبلغ ورودی مشتری پذیرفته نمی‌شود.
    /// </summary>
    public static CustomerPayment Open(
        Guid checkoutId,
        decimal amount,
        string currency,
        string providerCode,
        string idempotencyKey,
        IReadOnlyList<(Guid SellerOrderId, decimal Amount)> allocations,
        DateTimeOffset at)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("مبلغ پرداخت باید از تصویر سفارش مثبت باشد.");
        }

        if (allocations.Count == 0)
        {
            throw new InvalidOperationException("پرداخت بدون تخصیص فروشنده ساخته نمی‌شود.");
        }

        if (allocations.Sum(x => x.Amount) != amount)
        {
            throw new InvalidOperationException("جمع تخصیص فروشنده‌ها باید دقیقاً برابر مبلغ پرداخت باشد.");
        }

        if (allocations.Any(x => x.Amount <= 0))
        {
            throw new InvalidOperationException("تخصیص فروشنده نمی‌تواند صفر یا منفی باشد.");
        }

        var payment = new CustomerPayment
        {
            PaymentId = Guid.NewGuid(),
            CheckoutId = checkoutId,
            Amount = amount,
            Currency = currency,
            Status = PaymentStatus.Created,
            ProviderCode = providerCode,
            IdempotencyKey = idempotencyKey,
            CreatedAt = at,
            UpdatedAt = at,
        };
        foreach (var row in allocations)
        {
            payment._allocations.Add(PaymentAllocation.Create(payment.PaymentId, row.SellerOrderId, row.Amount, currency));
        }

        payment._domainEvents.Add(new PaymentCreatedDomainEvent(payment.PaymentId, payment.CheckoutId));
        return payment;
    }

    /// <summary>
    /// شروع درگاه را ثبت می‌کند؛ Succeeded نمی‌شود.
    /// </summary>
    public PaymentAttempt RecordInitiation(string requestReference, DateTimeOffset at)
    {
        if (Status is PaymentStatus.Succeeded or PaymentStatus.Cancelled)
        {
            throw new InvalidOperationException("پرداخت پایان‌یافته دوباره شروع نمی‌شود.");
        }

        var attempt = PaymentAttempt.Initiate(PaymentId, ProviderCode, requestReference, at);
        _attempts.Add(attempt);
        Status = PaymentStatus.Pending;
        UpdatedAt = at;
        _domainEvents.Add(new PaymentInitiatedDomainEvent(PaymentId, attempt.AttemptId, requestReference));
        return attempt;
    }

    /// <summary>
    /// فقط پس از Verify درگاه Succeeded می‌شود. متن callback کافی نیست.
    /// </summary>
    public bool ApplyVerifiedSuccess(Guid attemptId, string transactionReference, DateTimeOffset at)
    {
        if (Status == PaymentStatus.Succeeded)
        {
            return false;
        }

        var attempt = _attempts.Single(x => x.AttemptId == attemptId);
        attempt.MarkVerifiedSuccess(transactionReference, at);
        Status = PaymentStatus.Succeeded;
        CompletedAt = at;
        UpdatedAt = at;
        _domainEvents.Add(new PaymentSucceededDomainEvent(
            PaymentId,
            CheckoutId,
            Amount,
            Currency,
            transactionReference,
            _allocations.Select(x => x.SellerOrderId).ToArray()));
        return true;
    }

    /// <summary>
    /// شکست تأییدشدهٔ درگاه را اعمال می‌کند؛ سفارش را Paid نمی‌کند.
    /// </summary>
    public void ApplyVerifiedFailure(Guid attemptId, string? failureCode, DateTimeOffset at)
    {
        if (Status == PaymentStatus.Succeeded)
        {
            return;
        }

        var attempt = _attempts.Single(x => x.AttemptId == attemptId);
        attempt.MarkVerifiedFailure(failureCode, at);
        Status = PaymentStatus.Failed;
        UpdatedAt = at;
        _domainEvents.Add(new PaymentFailedDomainEvent(PaymentId, CheckoutId, failureCode));
    }

    /// <summary>
    /// تلاش تکراری با همان مرجع تراکنش را تشخیص می‌دهد.
    /// </summary>
    public bool AlreadySucceededWith(string transactionReference) =>
        Status == PaymentStatus.Succeeded
        && _attempts.Any(x => x.ProviderTransactionReference == transactionReference);

    /// <summary>
    /// تلاش بارگذاری‌شده از DbSet را به ریشه وصل می‌کند چون navigation در EF نادیده گرفته شده است.
    /// بدون این اتصال، Verify روی مجموعهٔ خالی شکست می‌خورد و متن callback جای حقیقت درگاه را می‌گیرد.
    /// </summary>
    public void AttachLoadedAttempt(PaymentAttempt attempt)
    {
        ArgumentNullException.ThrowIfNull(attempt);
        if (_attempts.All(x => x.AttemptId != attempt.AttemptId))
        {
            _attempts.Add(attempt);
        }
    }

    /// <summary>
    /// تخصیص‌های بارگذاری‌شده از DbSet را به ریشه وصل می‌کند چون navigation در EF نادیده گرفته شده است.
    /// بدون این اتصال، رویداد موفقیت SellerOrderIds خالی می‌سازد و تصویر Paid سفارش هرگز اعمال نمی‌شود.
    /// </summary>
    public void AttachLoadedAllocations(IEnumerable<PaymentAllocation> allocations)
    {
        ArgumentNullException.ThrowIfNull(allocations);
        foreach (var allocation in allocations)
        {
            if (_allocations.All(x => x.AllocationId != allocation.AllocationId))
            {
                _allocations.Add(allocation);
            }
        }
    }
}

/// <summary>
/// ایجاد پرداخت. سفارش را Paid نمی‌کند.
/// </summary>
public sealed class PaymentCreatedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد را می‌سازد.
    /// </summary>
    public PaymentCreatedDomainEvent(Guid paymentId, Guid checkoutId)
    {
        PaymentId = paymentId;
        CheckoutId = checkoutId;
        Metadata = EventMetadataFactory.ForDomain("payment.created.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>
    /// پرداخت.
    /// </summary>
    public Guid PaymentId { get; }

    /// <summary>
    /// checkout مرجع.
    /// </summary>
    public Guid CheckoutId { get; }
}

/// <summary>
/// شروع درگاه؛ موفقیت پرداخت نیست.
/// </summary>
public sealed class PaymentInitiatedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد را می‌سازد.
    /// </summary>
    public PaymentInitiatedDomainEvent(Guid paymentId, Guid attemptId, string providerRequestReference)
    {
        PaymentId = paymentId;
        AttemptId = attemptId;
        ProviderRequestReference = providerRequestReference;
        Metadata = EventMetadataFactory.ForDomain("payment.initiated.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>
    /// پرداخت.
    /// </summary>
    public Guid PaymentId { get; }

    /// <summary>
    /// تلاش.
    /// </summary>
    public Guid AttemptId { get; }

    /// <summary>
    /// مرجع درخواست درگاه.
    /// </summary>
    public string ProviderRequestReference { get; }
}

/// <summary>
/// موفقیت فقط پس از Verify درگاه.
/// </summary>
public sealed class PaymentSucceededDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد را می‌سازد.
    /// </summary>
    public PaymentSucceededDomainEvent(
        Guid paymentId,
        Guid checkoutId,
        decimal amount,
        string currency,
        string providerTransactionReference,
        IReadOnlyList<Guid> sellerOrderIds)
    {
        PaymentId = paymentId;
        CheckoutId = checkoutId;
        Amount = amount;
        Currency = currency;
        ProviderTransactionReference = providerTransactionReference;
        SellerOrderIds = sellerOrderIds;
        Metadata = EventMetadataFactory.ForDomain("payment.succeeded.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>
    /// پرداخت.
    /// </summary>
    public Guid PaymentId { get; }

    /// <summary>
    /// checkout.
    /// </summary>
    public Guid CheckoutId { get; }

    /// <summary>
    /// مبلغ تصویر.
    /// </summary>
    public decimal Amount { get; }

    /// <summary>
    /// ارز.
    /// </summary>
    public string Currency { get; }

    /// <summary>
    /// مرجع تراکنش تأییدشده.
    /// </summary>
    public string ProviderTransactionReference { get; }

    /// <summary>
    /// سفارش‌های فروشندهٔ تخصیص‌یافته. تصویر Paid فقط روی همین‌ها اعمال می‌شود.
    /// </summary>
    public IReadOnlyList<Guid> SellerOrderIds { get; }
}

/// <summary>
/// شکست پس از Verify. سفارش را Paid نمی‌کند.
/// </summary>
public sealed class PaymentFailedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد را می‌سازد.
    /// </summary>
    public PaymentFailedDomainEvent(Guid paymentId, Guid checkoutId, string? failureCode)
    {
        PaymentId = paymentId;
        CheckoutId = checkoutId;
        FailureCode = failureCode;
        Metadata = EventMetadataFactory.ForDomain("payment.failed.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>
    /// پرداخت.
    /// </summary>
    public Guid PaymentId { get; }

    /// <summary>
    /// checkout.
    /// </summary>
    public Guid CheckoutId { get; }

    /// <summary>
    /// کد شکست درگاه.
    /// </summary>
    public string? FailureCode { get; }
}
