namespace Tooba.Returns.Infrastructure.Persistence;

/// <summary>
/// TB-265-R2: the durable, serialized decision of one return case.
/// <para>
/// Approval and rejection used to read the mutable case state independently, so two operator requests could both
/// observe <c>Requested</c> and then approve (refund + release the source claim) while the other rejected (release the
/// claim). This row is the single decision authority: the FIRST committed decision for a case wins, a second request
/// with the opposite intent is refused before any money/stock/claim effect, and a concurrent request with the same
/// intent shares this row instead of minting a second decision.
/// </para>
/// </summary>
public sealed class ReturnDecisionRecord
{
    /// <summary>پروندهٔ مرجوعی مالک تصمیم (یک تصمیم برای هر پرونده).</summary>
    public Guid ReturnRequestId { get; init; }

    /// <summary>تصمیم پایدار: Approve | Reject.</summary>
    public string Decision { get; init; } = string.Empty;

    /// <summary>اپراتور تصمیم‌گیرنده.</summary>
    public Guid DecidedByUserId { get; init; }

    /// <summary>زمان تصمیم پایدار.</summary>
    public DateTimeOffset DecidedAt { get; init; }
}
