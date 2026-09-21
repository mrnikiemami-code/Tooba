using Tooba.BuildingBlocks;
using Tooba.Settlement.Domain.ValueObjects;

namespace Tooba.Settlement.Domain.Aggregates;

/// <summary>
/// Domain type.
/// </summary>
public sealed class PayoutAttempt
{
    private PayoutAttempt()
    {
    }

    /// <summary>شناسه تلاش.</summary>
    public Guid PayoutAttemptId { get; init; }

    /// <summary>درخواست مالک.</summary>
    public Guid PayoutRequestId { get; init; }

    /// <summary>وضعیت تلاش.</summary>
    public PayoutStatus Status { get; private set; }

    /// <summary>کلید idempotency درگاه.</summary>
    public string IdempotencyKey { get; init; } = string.Empty;

    /// <summary>مرجع درگاه.</summary>
    public string? ProviderReference { get; private set; }

    /// <summary>کد شکست.</summary>
    public string? FailureCode { get; private set; }

    /// <summary>زمان ایجاد.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>زمان تکمیل.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    internal static PayoutAttempt CreatePending(Guid id, Guid payoutRequestId, string idempotencyKey, DateTimeOffset now) =>
        new()
        {
            PayoutAttemptId = id,
            PayoutRequestId = payoutRequestId,
            Status = PayoutStatus.Processing,
            IdempotencyKey = idempotencyKey.Trim(),
            CreatedAt = now,
        };

    /// <summary>تلاش را موفق علامت می‌زند.</summary>
    public void MarkSucceeded(string providerReference, DateTimeOffset now)
    {
        Status = PayoutStatus.Succeeded;
        ProviderReference = providerReference.Trim();
        CompletedAt = now;
    }

    /// <summary>تلاش را شکست‌خورده علامت می‌زند.</summary>
    public void MarkFailed(string failureCode, DateTimeOffset now)
    {
        Status = PayoutStatus.Failed;
        FailureCode = failureCode.Trim();
        CompletedAt = now;
    }
}
