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

    /// <summary>
    /// بازگشت وجه شروع شده؛ هنوز موفق فرض نمی‌شود.
    /// </summary>
    RefundPending = 6,

    /// <summary>
    /// بازگشت وجه نزد درگاه تکمیل شده.
    /// </summary>
    Refunded = 7,

    /// <summary>
    /// بازگشت وجه شکست خورده؛ اقدام ادمین لازم است.
    /// </summary>
    RefundFailed = 8,
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
/// هدف مالی تخصیص پرداخت. فروشنده و هزینهٔ ارسال فروشگاه را قاطی نمی‌کند.
/// </summary>
public enum PaymentAllocationTargetKind
{
    /// <summary>سهم سفارش فروشنده.</summary>
    SellerOrder = 0,

    /// <summary>
    /// هزینهٔ ارسال متعلق به Store/platform؛ به SellerOrder نسبت داده نمی‌شود.
    /// </summary>
    StoreShipping = 1,
}

/// <summary>
/// تخصیص مبلغ یک پرداخت مشتری روی سفارش فروشنده یا هزینهٔ ارسال Store. تسویه/payout نیست.
/// </summary>
public sealed class PaymentAllocation
{
    /// <summary>
    /// شناسهٔ پایدار هدف StoreShipping (نه SellerOrder).
    /// </summary>
    public static readonly Guid StoreShippingTargetId = Guid.Parse("00000000-0000-7000-9000-00000000a11c");

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
    /// نوع هدف تخصیص.
    /// </summary>
    public PaymentAllocationTargetKind TargetKind { get; init; }

    /// <summary>
    /// سفارش فروشنده وقتی TargetKind=SellerOrder؛ برای StoreShipping همان StoreShippingTargetId.
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
    /// آیا این تخصیص متعلق به SellerOrder است؟
    /// </summary>
    public bool IsSellerOrder => TargetKind == PaymentAllocationTargetKind.SellerOrder;

    /// <summary>
    /// تخصیص فروشنده را می‌سازد.
    /// </summary>
    public static PaymentAllocation Create(Guid paymentId, Guid sellerOrderId, decimal amount, string currency)
        => Create(paymentId, PaymentAllocationTargetKind.SellerOrder, sellerOrderId, amount, currency);

    /// <summary>
    /// تخصیص هزینهٔ ارسال Store را می‌سازد.
    /// </summary>
    public static PaymentAllocation CreateStoreShipping(Guid paymentId, decimal amount, string currency)
        => Create(paymentId, PaymentAllocationTargetKind.StoreShipping, StoreShippingTargetId, amount, currency);

    /// <summary>
    /// تخصیص را می‌سازد.
    /// </summary>
    public static PaymentAllocation Create(
        Guid paymentId,
        PaymentAllocationTargetKind targetKind,
        Guid targetId,
        decimal amount,
        string currency)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("تخصیص پرداخت باید مبلغ مثبت داشته باشد.");
        }

        if (targetKind == PaymentAllocationTargetKind.SellerOrder && targetId == Guid.Empty)
        {
            throw new InvalidOperationException("تخصیص فروشنده بدون SellerOrderId ساخته نمی‌شود.");
        }

        if (targetKind == PaymentAllocationTargetKind.StoreShipping && targetId != StoreShippingTargetId)
        {
            throw new InvalidOperationException("هدف StoreShipping باید شناسهٔ پایدار Store باشد.");
        }

        return new PaymentAllocation
        {
            AllocationId = Guid.NewGuid(),
            PaymentId = paymentId,
            TargetKind = targetKind,
            SellerOrderId = targetId,
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
    /// شماره پیگیری واریز مشتری (کارت‌به‌کارت). شماره پیگیری سفارش نیست.
    /// </summary>
    public string? CustomerTransferReference { get; private set; }

    /// <summary>
    /// شناسهٔ دارایی رسانه برای مدرک واریز؛ مسیر فایل خام ذخیره نمی‌شود.
    /// </summary>
    public Guid? ProofMediaAssetId { get; private set; }

    /// <summary>
    /// زمان ثبت مدرک/پیگیری توسط مشتری.
    /// </summary>
    public DateTimeOffset? EvidenceSubmittedAt { get; private set; }

    /// <summary>
    /// سقف طول شماره پیگیری پرداخت.
    /// </summary>
    public const int CustomerTransferReferenceMaxLength = 64;

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

    /// <summary>
    /// مدرک کارت‌به‌کارت را روی تلاش Initiated ثبت می‌کند؛ تلاش تاریخی را بازنویسی نمی‌کند.
    /// </summary>
    public void SubmitCustomerEvidence(string transferReference, Guid? proofMediaAssetId, DateTimeOffset at)
    {
        var trimmed = (transferReference ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new InvalidOperationException("شماره پیگیری پرداخت الزامی است.");
        }

        if (trimmed.Length > CustomerTransferReferenceMaxLength)
        {
            throw new InvalidOperationException("شماره پیگیری پرداخت الزامی است.");
        }

        if (Status != PaymentAttemptStatus.Initiated)
        {
            throw new InvalidOperationException("payment.manual.evidence.immutable");
        }

        if (EvidenceSubmittedAt is not null)
        {
            var sameRef = string.Equals(CustomerTransferReference, trimmed, StringComparison.Ordinal);
            var sameProof = ProofMediaAssetId == proofMediaAssetId;
            if (sameRef && sameProof)
            {
                return;
            }

            throw new InvalidOperationException("payment.manual.evidence.duplicate");
        }

        CustomerTransferReference = trimmed;
        ProofMediaAssetId = proofMediaAssetId;
        EvidenceSubmittedAt = at;
    }
}

/// <summary>
/// دارایی مدرک واریز متصل به پرداخت؛ فقط MediaAssetId نگه داشته می‌شود.
/// </summary>
public sealed class PaymentProofAsset
{
    /// <summary>سازندهٔ EF.</summary>
    private PaymentProofAsset()
    {
    }

    /// <summary>شناسهٔ ردیف اتصال.</summary>
    public Guid ProofAssetRowId { get; init; }

    /// <summary>پرداخت مالک.</summary>
    public Guid PaymentId { get; init; }

    /// <summary>دارایی Media آپلودشده برای همین پرداخت.</summary>
    public Guid MediaAssetId { get; init; }

    /// <summary>زمان اتصال.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>اتصال مدرک را می‌سازد.</summary>
    public static PaymentProofAsset Attach(Guid paymentId, Guid mediaAssetId, DateTimeOffset at)
    {
        if (paymentId == Guid.Empty || mediaAssetId == Guid.Empty)
        {
            throw new InvalidOperationException("payment.proof.invalid");
        }

        return new PaymentProofAsset
        {
            ProofAssetRowId = Guid.NewGuid(),
            PaymentId = paymentId,
            MediaAssetId = mediaAssetId,
            CreatedAt = at,
        };
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
    /// مهلت پرداخت نشده از سیاست Settings؛ پس از مدرک دستی یا موفقیت پاک می‌شود.
    /// </summary>
    public DateTimeOffset? UnpaidTimeoutAt { get; private set; }

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
        => Open(
            checkoutId,
            amount,
            currency,
            providerCode,
            idempotencyKey,
            allocations
                .Select(x => (PaymentAllocationTargetKind.SellerOrder, x.SellerOrderId, x.Amount))
                .ToArray(),
            at);

    /// <summary>
    /// پرداخت را از تصویر سفارش می‌سازد. مبلغ ورودی مشتری پذیرفته نمی‌شود.
    /// تخصیص‌ها می‌توانند SellerOrder یا StoreShipping باشند؛ جمع باید برابر مبلغ باشد.
    /// </summary>
    public static CustomerPayment Open(
        Guid checkoutId,
        decimal amount,
        string currency,
        string providerCode,
        string idempotencyKey,
        IReadOnlyList<(PaymentAllocationTargetKind TargetKind, Guid TargetId, decimal Amount)> allocations,
        DateTimeOffset at)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("مبلغ پرداخت باید از تصویر سفارش مثبت باشد.");
        }

        if (allocations.Count == 0)
        {
            throw new InvalidOperationException("پرداخت بدون تخصیص ساخته نمی‌شود.");
        }

        if (allocations.Sum(x => x.Amount) != amount)
        {
            throw new InvalidOperationException("جمع تخصیص‌ها باید دقیقاً برابر مبلغ پرداخت باشد.");
        }

        if (allocations.Any(x => x.Amount <= 0))
        {
            throw new InvalidOperationException("تخصیص نمی‌تواند صفر یا منفی باشد.");
        }

        if (!allocations.Any(x => x.TargetKind == PaymentAllocationTargetKind.SellerOrder))
        {
            throw new InvalidOperationException("پرداخت بدون تخصیص فروشنده ساخته نمی‌شود.");
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
            payment._allocations.Add(PaymentAllocation.Create(
                payment.PaymentId,
                row.TargetKind,
                row.TargetId,
                row.Amount,
                currency));
        }

        payment._domainEvents.Add(new PaymentCreatedDomainEvent(payment.PaymentId, payment.CheckoutId));
        return payment;
    }

    /// <summary>
    /// شروع درگاه را ثبت می‌کند؛ Succeeded نمی‌شود.
    /// </summary>
    public PaymentAttempt RecordInitiation(string requestReference, DateTimeOffset at)
    {
        if (Status is PaymentStatus.Succeeded
            or PaymentStatus.Cancelled
            or PaymentStatus.RefundPending
            or PaymentStatus.Refunded
            or PaymentStatus.RefundFailed)
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
    /// شماره پیگیری و مدرک کارت‌به‌کارت را روی آخرین تلاش Initiated ثبت می‌کند.
    /// </summary>
    public void SubmitManualEvidence(string transferReference, Guid? proofMediaAssetId, DateTimeOffset at)
    {
        if (!string.Equals(ProviderCode, "manual", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("payment.method.not_manual");
        }

        if (Status != PaymentStatus.Pending)
        {
            throw new InvalidOperationException("payment.manual.submit.invalid_state");
        }

        var attempt = _attempts
            .Where(x => x.Status == PaymentAttemptStatus.Initiated)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("payment.attempt.missing");
        attempt.SubmitCustomerEvidence(transferReference, proofMediaAssetId, at);
        UnpaidTimeoutAt = null;
        UpdatedAt = at;
    }

    /// <summary>مهلت unpaid را از سیاست Settings می‌نویسد؛ رزرو را زنده نمی‌کند.</summary>
    public void AssignUnpaidTimeout(DateTimeOffset timeoutAt, DateTimeOffset at)
    {
        UnpaidTimeoutAt = timeoutAt;
        UpdatedAt = at;
    }

    /// <summary>
    /// مهلت پرداخت‌نشده را اعمال می‌کند. Succeeded، بازبینی مدرک دستی، و Cancel کاربر را لمس نمی‌کند.
    /// </summary>
    public bool ExpireUnpaidTimeout(DateTimeOffset at)
    {
        if (Status == PaymentStatus.Expired)
        {
            return false;
        }

        if (Status is PaymentStatus.Succeeded
            or PaymentStatus.RefundPending
            or PaymentStatus.Refunded
            or PaymentStatus.RefundFailed
            or PaymentStatus.Cancelled)
        {
            return false;
        }

        if (HasActiveManualEvidence())
        {
            return false;
        }

        Status = PaymentStatus.Expired;
        UnpaidTimeoutAt = null;
        UpdatedAt = at;
        return true;
    }

    /// <summary>مدرک کارت‌به‌کارت ثبت‌شده روی تلاش Initiated جاری.</summary>
    public bool HasActiveManualEvidence() =>
        _attempts.Any(x =>
            x.Status == PaymentAttemptStatus.Initiated
            && !string.IsNullOrWhiteSpace(x.CustomerTransferReference));

    /// <summary>
    /// فقط پس از Verify درگاه Succeeded می‌شود. متن callback کافی نیست.
    /// </summary>
    public bool ApplyVerifiedSuccess(Guid attemptId, string transactionReference, DateTimeOffset at)
    {
        if (Status is PaymentStatus.Succeeded
            or PaymentStatus.RefundPending
            or PaymentStatus.Refunded
            or PaymentStatus.RefundFailed)
        {
            return false;
        }

        var attempt = _attempts.Single(x => x.AttemptId == attemptId);
        attempt.MarkVerifiedSuccess(transactionReference, at);
        Status = PaymentStatus.Succeeded;
        CompletedAt = at;
        UnpaidTimeoutAt = null;
        UpdatedAt = at;
        _domainEvents.Add(new PaymentSucceededDomainEvent(
            PaymentId,
            CheckoutId,
            Amount,
            Currency,
            transactionReference,
            _allocations.Where(x => x.IsSellerOrder).Select(x => x.SellerOrderId).ToArray()));
        return true;
    }

    /// <summary>
    /// شکست تأییدشدهٔ درگاه را اعمال می‌کند؛ سفارش را Paid نمی‌کند.
    /// </summary>
    public void ApplyVerifiedFailure(Guid attemptId, string? failureCode, DateTimeOffset at)
    {
        if (Status is PaymentStatus.Succeeded
            or PaymentStatus.RefundPending
            or PaymentStatus.Refunded
            or PaymentStatus.RefundFailed)
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
    /// رد واریز دستی را به انتظار تأیید برمی‌گرداند؛ Succeeded یا رویداد موفقیت نمی‌سازد.
    /// </summary>
    public PaymentAttempt RestoreRejectedManualToPending(DateTimeOffset at)
    {
        if (!string.Equals(ProviderCode, "manual", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("payment.restore.not_manual");
        }

        if (Status == PaymentStatus.Pending)
        {
            var latest = _attempts.OrderByDescending(x => x.CreatedAt).FirstOrDefault();
            var hasRejected = _attempts.Any(x =>
                x.Status == PaymentAttemptStatus.VerifiedFailed
                && string.Equals(x.FailureCode, "MANUAL_DEPOSIT_REJECTED", StringComparison.Ordinal));
            if (hasRejected && latest is not null && latest.Status == PaymentAttemptStatus.Initiated)
            {
                return latest;
            }

            throw new InvalidOperationException("payment.restore.invalid_state");
        }

        if (Status == PaymentStatus.Succeeded)
        {
            throw new InvalidOperationException("payment.restore.already_succeeded");
        }

        if (Status != PaymentStatus.Failed)
        {
            throw new InvalidOperationException("payment.restore.invalid_state");
        }

        var rejected = _attempts
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefault(x => x.Status == PaymentAttemptStatus.VerifiedFailed
                && string.Equals(x.FailureCode, "MANUAL_DEPOSIT_REJECTED", StringComparison.Ordinal));
        if (rejected is null)
        {
            throw new InvalidOperationException("payment.restore.invalid_state");
        }

        var attempt = RecordInitiation($"manual-restore-{PaymentId:N}-{at.UtcTicks}", at);
        _domainEvents.Add(new PaymentManualDepositRestoredDomainEvent(PaymentId, CheckoutId, attempt.AttemptId));
        return attempt;
    }

    /// <summary>
    /// تأیید واریز دستی را به انتظار تأیید برمی‌گرداند؛ Paid نمی‌ماند و رویداد موفقیت جدید نمی‌سازد.
    /// </summary>
    public PaymentAttempt UnconfirmManualDeposit(DateTimeOffset at)
    {
        if (!string.Equals(ProviderCode, "manual", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("payment.unconfirm.not_manual");
        }

        if (Status == PaymentStatus.Pending)
        {
            var latest = _attempts.OrderByDescending(x => x.CreatedAt).FirstOrDefault();
            var hadSuccess = _attempts.Any(x => x.Status == PaymentAttemptStatus.VerifiedSucceeded);
            if (hadSuccess && latest is not null && latest.Status == PaymentAttemptStatus.Initiated)
            {
                return latest;
            }

            throw new InvalidOperationException("payment.unconfirm.invalid_state");
        }

        if (Status != PaymentStatus.Succeeded)
        {
            throw new InvalidOperationException("payment.unconfirm.invalid_state");
        }

        CompletedAt = null;
        Status = PaymentStatus.Pending;
        var attempt = RecordInitiation($"manual-unconfirm-{PaymentId:N}-{at.UtcTicks}", at);
        _domainEvents.Add(new PaymentManualDepositUnconfirmedDomainEvent(PaymentId, CheckoutId, attempt.AttemptId));
        return attempt;
    }

    /// <summary>
    /// تلاش تکراری با همان مرجع تراکنش را تشخیص می‌دهد.
    /// </summary>
    public bool AlreadySucceededWith(string transactionReference) =>
        Status == PaymentStatus.Succeeded
        && _attempts.Any(x => x.ProviderTransactionReference == transactionReference);

    /// <summary>
    /// پرداخت موفق‌نشده را با لغو سفارش می‌بندد تا Confirm/Reject نماند.
    /// </summary>
    public void CloseForOrderCancel(DateTimeOffset at)
    {
        if (Status is PaymentStatus.Cancelled
            or PaymentStatus.RefundPending
            or PaymentStatus.Refunded
            or PaymentStatus.RefundFailed)
        {
            return;
        }

        if (Status == PaymentStatus.Succeeded)
        {
            throw new InvalidOperationException("payment.cancel.requires_refund");
        }

        Status = PaymentStatus.Cancelled;
        UpdatedAt = at;
        _domainEvents.Add(new PaymentFailedDomainEvent(PaymentId, CheckoutId, "ORDER_CANCELLED"));
    }

    /// <summary>
    /// شروع workflow بازگشت وجه پس از لغو سفارش؛ موفقیت درگاه را فرض نمی‌کند.
    /// </summary>
    public void BeginOrderCancelRefund(DateTimeOffset at)
    {
        if (Status is PaymentStatus.RefundPending or PaymentStatus.Refunded or PaymentStatus.RefundFailed)
        {
            return;
        }

        if (Status != PaymentStatus.Succeeded)
        {
            throw new InvalidOperationException("payment.refund.invalid_state");
        }

        Status = PaymentStatus.RefundPending;
        UpdatedAt = at;
        _domainEvents.Add(new PaymentRefundPendingDomainEvent(PaymentId, CheckoutId));
    }

    /// <summary>بازگشت وجه درگاه را موفق ثبت می‌کند.</summary>
    public void MarkRefunded(DateTimeOffset at)
    {
        if (Status == PaymentStatus.Refunded)
        {
            return;
        }

        if (Status is not (PaymentStatus.RefundPending or PaymentStatus.Succeeded))
        {
            throw new InvalidOperationException("payment.refund.invalid_state");
        }

        Status = PaymentStatus.Refunded;
        UpdatedAt = at;
        _domainEvents.Add(new PaymentRefundedDomainEvent(PaymentId, CheckoutId, Amount, Currency));
    }

    /// <summary>شکست بازگشت وجه؛ سفارش Cancelled می‌ماند.</summary>
    public void MarkRefundFailed(string? failureCode, DateTimeOffset at)
    {
        if (Status == PaymentStatus.RefundFailed)
        {
            return;
        }

        if (Status != PaymentStatus.RefundPending)
        {
            throw new InvalidOperationException("payment.refund.invalid_state");
        }

        Status = PaymentStatus.RefundFailed;
        UpdatedAt = at;
        _domainEvents.Add(new PaymentRefundFailedDomainEvent(PaymentId, CheckoutId, failureCode));
    }

    /// <summary>
    /// بازگردانی لغو: اگر Refund نهایی نشده باشد، پرداخت به وضعیت عملیاتی قبل از Cancel برمی‌گردد.
    /// رویداد موفقیت جدید نمی‌سازد تا accrual تکرار نشود.
    /// </summary>
    public void RestoreAfterOrderCancelRestore(DateTimeOffset at)
    {
        if (Status is PaymentStatus.Succeeded
            or PaymentStatus.Pending
            or PaymentStatus.Created
            or PaymentStatus.Failed)
        {
            return;
        }

        if (Status == PaymentStatus.Refunded)
        {
            throw new InvalidOperationException("payment.restore.refund_completed");
        }

        if (Status is PaymentStatus.RefundPending or PaymentStatus.RefundFailed)
        {
            Status = PaymentStatus.Succeeded;
            UpdatedAt = at;
            return;
        }

        if (Status is PaymentStatus.Cancelled or PaymentStatus.Expired)
        {
            Status = ResolveOperationalStatusBeforeOrderCancel();
            UpdatedAt = at;
        }
    }

    private PaymentStatus ResolveOperationalStatusBeforeOrderCancel()
    {
        if (_attempts.Count == 0)
        {
            return PaymentStatus.Created;
        }

        var latest = _attempts
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.AttemptId)
            .First();
        if (latest.Status == PaymentAttemptStatus.VerifiedFailed
            && string.Equals(latest.FailureCode, "MANUAL_DEPOSIT_REJECTED", StringComparison.Ordinal))
        {
            return PaymentStatus.Failed;
        }

        return PaymentStatus.Pending;
    }

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

/// <summary>
/// بازگرداندن رد واریز دستی به انتظار تأیید. Paid نمی‌کند.
/// </summary>
public sealed class PaymentManualDepositRestoredDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد را می‌سازد.
    /// </summary>
    public PaymentManualDepositRestoredDomainEvent(Guid paymentId, Guid checkoutId, Guid attemptId)
    {
        PaymentId = paymentId;
        CheckoutId = checkoutId;
        AttemptId = attemptId;
        Metadata = EventMetadataFactory.ForDomain("payment.manual_deposit.restored.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>پرداخت.</summary>
    public Guid PaymentId { get; }

    /// <summary>checkout.</summary>
    public Guid CheckoutId { get; }

    /// <summary>تلاش جدید انتظار تأیید.</summary>
    public Guid AttemptId { get; }
}

/// <summary>
/// برگشت تأیید واریز دستی به انتظار تأیید. Paid را نگه نمی‌دارد.
/// </summary>
public sealed class PaymentManualDepositUnconfirmedDomainEvent : IDomainEvent
{
    /// <summary>رویداد را می‌سازد.</summary>
    public PaymentManualDepositUnconfirmedDomainEvent(Guid paymentId, Guid checkoutId, Guid attemptId)
    {
        PaymentId = paymentId;
        CheckoutId = checkoutId;
        AttemptId = attemptId;
        Metadata = EventMetadataFactory.ForDomain("payment.manual_deposit.unconfirmed.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>پرداخت.</summary>
    public Guid PaymentId { get; }

    /// <summary>checkout.</summary>
    public Guid CheckoutId { get; }

    /// <summary>تلاش جدید انتظار تأیید.</summary>
    public Guid AttemptId { get; }
}

/// <summary>شروع بازگشت وجه پس از لغو سفارش؛ موفقیت درگاه نیست.</summary>
public sealed class PaymentRefundPendingDomainEvent : IDomainEvent
{
    /// <summary>رویداد را می‌سازد.</summary>
    public PaymentRefundPendingDomainEvent(Guid paymentId, Guid checkoutId)
    {
        PaymentId = paymentId;
        CheckoutId = checkoutId;
        Metadata = EventMetadataFactory.ForDomain("payment.refund_pending.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>پرداخت.</summary>
    public Guid PaymentId { get; }

    /// <summary>checkout.</summary>
    public Guid CheckoutId { get; }
}

/// <summary>بازگشت وجه نزد درگاه تکمیل شد.</summary>
public sealed class PaymentRefundedDomainEvent : IDomainEvent
{
    /// <summary>رویداد را می‌سازد.</summary>
    public PaymentRefundedDomainEvent(Guid paymentId, Guid checkoutId, decimal amount, string currency)
    {
        PaymentId = paymentId;
        CheckoutId = checkoutId;
        Amount = amount;
        Currency = currency;
        Metadata = EventMetadataFactory.ForDomain("payment.refunded.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>پرداخت.</summary>
    public Guid PaymentId { get; }

    /// <summary>checkout.</summary>
    public Guid CheckoutId { get; }

    /// <summary>مبلغ.</summary>
    public decimal Amount { get; }

    /// <summary>ارز.</summary>
    public string Currency { get; }
}

/// <summary>شکست بازگشت وجه؛ سفارش Cancelled می‌ماند.</summary>
public sealed class PaymentRefundFailedDomainEvent : IDomainEvent
{
    /// <summary>رویداد را می‌سازد.</summary>
    public PaymentRefundFailedDomainEvent(Guid paymentId, Guid checkoutId, string? failureCode)
    {
        PaymentId = paymentId;
        CheckoutId = checkoutId;
        FailureCode = failureCode;
        Metadata = EventMetadataFactory.ForDomain("payment.refund_failed.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>پرداخت.</summary>
    public Guid PaymentId { get; }

    /// <summary>checkout.</summary>
    public Guid CheckoutId { get; }

    /// <summary>کد شکست.</summary>
    public string? FailureCode { get; }
}
