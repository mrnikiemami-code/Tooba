using Tooba.BuildingBlocks;
using Tooba.Returns.Domain.Events;
using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Domain.Aggregates;


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
        Guid refundAttemptId,
        Guid returnRequestId,
        Guid paymentId,
        decimal amount,
        string currency,
        string idempotencyKey,
        DateTimeOffset now) =>
        new()
        {
            RefundAttemptId = refundAttemptId,
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
