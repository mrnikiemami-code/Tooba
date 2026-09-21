using Tooba.BuildingBlocks;
using Tooba.Payment.Domain.Events;
using Tooba.Payment.Domain.ValueObjects;

namespace Tooba.Payment.Domain.Aggregates;

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
    public static CustomerPayment Create(
        Guid paymentId,
        Guid checkoutId,
        decimal amount,
        string currency,
        string providerCode,
        string idempotencyKey,
        IReadOnlyList<(Guid SellerOrderId, decimal Amount, Guid AllocationId)> allocations,
        DateTimeOffset at)
        => Open(
            paymentId,
            checkoutId,
            amount,
            currency,
            providerCode,
            idempotencyKey,
            allocations
                .Select(x => (PaymentAllocationTargetKind.SellerOrder, x.SellerOrderId, x.Amount, x.AllocationId))
                .ToArray(),
            at);

    /// <summary>
    /// پرداخت را از تصویر سفارش می‌سازد. مبلغ ورودی مشتری پذیرفته نمی‌شود.
    /// تخصیص‌ها می‌توانند SellerOrder یا StoreShipping باشند؛ جمع باید برابر مبلغ باشد.
    /// </summary>
    public static CustomerPayment Open(
        Guid paymentId,
        Guid checkoutId,
        decimal amount,
        string currency,
        string providerCode,
        string idempotencyKey,
        IReadOnlyList<(PaymentAllocationTargetKind TargetKind, Guid TargetId, decimal Amount, Guid AllocationId)> allocations,
        DateTimeOffset at)
    {
        if (paymentId == Guid.Empty)
        {
            throw new InvalidOperationException("payment.ids_required");
        }

        if (amount <= 0)
        {
            throw new InvalidOperationException("payment.amount_positive");
        }

        if (allocations.Count == 0)
        {
            throw new InvalidOperationException("payment.allocations_required");
        }

        if (allocations.Sum(x => x.Amount) != amount)
        {
            throw new InvalidOperationException("payment.allocations_sum_mismatch");
        }

        if (allocations.Any(x => x.Amount <= 0))
        {
            throw new InvalidOperationException("payment.allocation.non_positive");
        }

        if (!allocations.Any(x => x.TargetKind == PaymentAllocationTargetKind.SellerOrder))
        {
            throw new InvalidOperationException("payment.seller_allocation_required");
        }

        var payment = new CustomerPayment
        {
            PaymentId = paymentId,
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
                row.AllocationId,
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
    public PaymentAttempt RecordInitiation(Guid attemptId, string requestReference, DateTimeOffset at)
    {
        if (Status is PaymentStatus.Succeeded
            or PaymentStatus.Cancelled
            or PaymentStatus.RefundPending
            or PaymentStatus.Refunded
            or PaymentStatus.RefundFailed)
        {
            throw new InvalidOperationException("payment.terminal_no_reinitiate");
        }

        var attempt = PaymentAttempt.Initiate(attemptId, PaymentId, ProviderCode, requestReference, at);
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
    public PaymentAttempt RestoreRejectedManualToPending(Guid attemptId, DateTimeOffset at)
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

        var attempt = RecordInitiation(attemptId, $"manual-restore-{PaymentId:N}-{at.UtcTicks}", at);
        _domainEvents.Add(new PaymentManualDepositRestoredDomainEvent(PaymentId, CheckoutId, attempt.AttemptId));
        return attempt;
    }

    /// <summary>
    /// تأیید واریز دستی را به انتظار تأیید برمی‌گرداند؛ Paid نمی‌ماند و رویداد موفقیت جدید نمی‌سازد.
    /// </summary>
    public PaymentAttempt UnconfirmManualDeposit(Guid attemptId, DateTimeOffset at)
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
        var attempt = RecordInitiation(attemptId, $"manual-unconfirm-{PaymentId:N}-{at.UtcTicks}", at);
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
