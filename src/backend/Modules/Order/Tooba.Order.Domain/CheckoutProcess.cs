namespace Tooba.Order.Domain;

/// <summary>وضعیت فرآیند checkout؛ با وضعیت کسب‌وکار SellerOrder یکی نیست.</summary>
public enum CheckoutProcessStatus
{
    /// <summary>فرآیند ایجاد شده.</summary>
    Started = 0,

    /// <summary>اعتبارسنجی قیمت/پیشنهاد/مالیات.</summary>
    Validating = 1,

    /// <summary>رزرو موجودی.</summary>
    InventoryReserving = 2,

    /// <summary>پایدارسازی سفارش.</summary>
    OrderPersisting = 3,

    /// <summary>تبدیل سبد.</summary>
    CartCommitting = 4,

    /// <summary>ارسال موفق؛ در انتظار پرداخت (مرحلهٔ submit).</summary>
    PaymentPending = 5,

    /// <summary>شکست ترمینال فرآیند submit.</summary>
    Failed = 6,
}

/// <summary>هویت پایدار فرآیند checkout برای idempotency و بازیابی؛ موتور Saga نیست.</summary>
public sealed class CheckoutProcess
{
    private CheckoutProcess()
    {
    }

    /// <summary>شناسهٔ فرآیند.</summary>
    public Guid ProcessId { get; init; }

    /// <summary>کلید تکرارناپذیری ارسال (همان IdempotencyKey کلاینت).</summary>
    public string SubmissionIdempotencyKey { get; private set; } = string.Empty;

    /// <summary>سبد مبدأ.</summary>
    public Guid CartId { get; private set; }

    /// <summary>شناسهٔ checkout وقتی ساخته شد.</summary>
    public Guid? CheckoutId { get; private set; }

    /// <summary>وضعیت فرآیند.</summary>
    public CheckoutProcessStatus Status { get; private set; }

    /// <summary>همبستگی بازیابی.</summary>
    public string CorrelationId { get; private set; } = string.Empty;

    /// <summary>شروع.</summary>
    public DateTimeOffset StartedAt { get; init; }

    /// <summary>آخرین به‌روزرسانی.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>کد خطای معنایی اختیاری.</summary>
    public string? FailureCode { get; private set; }

    /// <summary>فرآیند جدید را شروع می‌کند.</summary>
    public static CheckoutProcess Start(
        Guid processId,
        string submissionIdempotencyKey,
        Guid cartId,
        string correlationId,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(submissionIdempotencyKey))
        {
            throw new InvalidOperationException("checkout_process.idempotency_key.required");
        }

        if (cartId == Guid.Empty)
        {
            throw new InvalidOperationException("checkout_process.cart_id.required");
        }

        return new CheckoutProcess
        {
            ProcessId = processId,
            SubmissionIdempotencyKey = submissionIdempotencyKey.Trim(),
            CartId = cartId,
            Status = CheckoutProcessStatus.Started,
            CorrelationId = string.IsNullOrWhiteSpace(correlationId)
                ? $"checkout-process:{processId:N}"
                : correlationId.Trim(),
            StartedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>به Validating می‌رود.</summary>
    public void MarkValidating(DateTimeOffset now) => TransitionTo(CheckoutProcessStatus.Validating, now);

    /// <summary>به InventoryReserving می‌رود.</summary>
    public void MarkInventoryReserving(DateTimeOffset now) => TransitionTo(CheckoutProcessStatus.InventoryReserving, now);

    /// <summary>به OrderPersisting می‌رود و CheckoutId را می‌بندد.</summary>
    public void MarkOrderPersisting(Guid checkoutId, DateTimeOffset now)
    {
        if (checkoutId == Guid.Empty)
        {
            throw new InvalidOperationException("checkout_process.checkout_id.required");
        }

        CheckoutId = checkoutId;
        TransitionTo(CheckoutProcessStatus.OrderPersisting, now);
    }

    /// <summary>به CartCommitting می‌رود.</summary>
    public void MarkCartCommitting(DateTimeOffset now) => TransitionTo(CheckoutProcessStatus.CartCommitting, now);

    /// <summary>ارسال موفق؛ PaymentPending.</summary>
    public void MarkPaymentPending(DateTimeOffset now) => TransitionTo(CheckoutProcessStatus.PaymentPending, now);

    /// <summary>شکست ترمینال.</summary>
    public void MarkFailed(string failureCode, DateTimeOffset now)
    {
        if (Status is CheckoutProcessStatus.PaymentPending)
        {
            throw new InvalidOperationException("checkout_process.transition.invalid");
        }

        FailureCode = string.IsNullOrWhiteSpace(failureCode) ? "checkout_process.failed" : failureCode.Trim();
        TransitionTo(CheckoutProcessStatus.Failed, now);
    }

    /// <summary>آیا submit موفق و ترمینال است؟</summary>
    public bool IsSubmitSucceeded => Status == CheckoutProcessStatus.PaymentPending;

    private void TransitionTo(CheckoutProcessStatus next, DateTimeOffset now)
    {
        if (Status == next)
        {
            UpdatedAt = now;
            return;
        }

        if (!IsAllowed(Status, next))
        {
            throw new InvalidOperationException("checkout_process.transition.invalid");
        }

        Status = next;
        UpdatedAt = now;
    }

    private static bool IsAllowed(CheckoutProcessStatus from, CheckoutProcessStatus to) =>
        (from, to) switch
        {
            (CheckoutProcessStatus.Started, CheckoutProcessStatus.Validating) => true,
            (CheckoutProcessStatus.Started, CheckoutProcessStatus.Failed) => true,
            (CheckoutProcessStatus.Validating, CheckoutProcessStatus.InventoryReserving) => true,
            (CheckoutProcessStatus.Validating, CheckoutProcessStatus.Failed) => true,
            (CheckoutProcessStatus.InventoryReserving, CheckoutProcessStatus.OrderPersisting) => true,
            (CheckoutProcessStatus.InventoryReserving, CheckoutProcessStatus.Failed) => true,
            (CheckoutProcessStatus.OrderPersisting, CheckoutProcessStatus.CartCommitting) => true,
            (CheckoutProcessStatus.OrderPersisting, CheckoutProcessStatus.Failed) => true,
            (CheckoutProcessStatus.CartCommitting, CheckoutProcessStatus.PaymentPending) => true,
            (CheckoutProcessStatus.CartCommitting, CheckoutProcessStatus.Failed) => true,
            (CheckoutProcessStatus.Failed, CheckoutProcessStatus.Started) => false,
            (CheckoutProcessStatus.PaymentPending, _) => false,
            _ => false,
        };
}
