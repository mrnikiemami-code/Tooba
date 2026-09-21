using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Application.Models;


/// <summary>
/// snapshot تلاش refund.
/// </summary>
public sealed record RefundAttemptSnapshot(
    Guid RefundAttemptId,
    Guid PaymentId,
    decimal Amount,
    string Currency,
    RefundAttemptStatus Status,
    string IdempotencyKey,
    string? ProviderReference,
    string? FailureCode,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);
