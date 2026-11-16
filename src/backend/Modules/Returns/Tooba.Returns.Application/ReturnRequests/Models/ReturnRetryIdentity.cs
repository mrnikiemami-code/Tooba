using System.Security.Cryptography;
using System.Text;

namespace Tooba.Returns.Application.ReturnRequests.Models;

/// <summary>
/// TB-275 (C2): the ONE deterministic identity rule of a reviewed definite-failure retry.
/// <para>
/// A retry is a deliberate answer to a SPECIFIC stored failure. Deriving the child attempt's identity from
/// <c>(case, reviewed failure)</c> (instead of a counter or a random key) is what makes a duplicate POST, a lost
/// response or a remount resolve to the SAME child attempt: the primary key itself arbitrates and no second table,
/// counter or provider key registry is introduced. The provider-facing idempotency key is derived from the reviewed
/// failure alone, so the same reviewed intent can never be charged twice under two keys.
/// </para>
/// <para>
/// The read policy and the owning command both call this type, so the failed attempt a workspace offers and the one
/// the retry action actually answers for can never disagree.
/// </para>
/// </summary>
public static class ReturnRetryIdentity
{
    /// <summary>The deterministic child attempt id of one reviewed failure of one case.</summary>
    public static Guid DeriveAttemptId(Guid returnRequestId, Guid failedAttemptId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(
            $"tb275-refund-retry:{returnRequestId:N}:{failedAttemptId:N}"));
        return new Guid(hash.AsSpan(0, 16));
    }

    /// <summary>The deterministic provider-facing idempotency key of that same child attempt.</summary>
    public static string DeriveIdempotencyKey(Guid failedAttemptId) => $"refund-retry-{failedAttemptId:N}";

    /// <summary>
    /// The CURRENT reviewable definite failure of one case: a bound FAILED attempt whose own derived child does not
    /// exist yet. A failure that already produced a child is history — retrying it again would answer an older
    /// question — so only the un-retried failure is eligible for a NEW deliberate review.
    /// </summary>
    public static Guid? CurrentReviewableFailureId(Guid returnRequestId, IEnumerable<RefundAttemptSnapshot> attempts)
    {
        ArgumentNullException.ThrowIfNull(attempts);
        var all = attempts as IReadOnlyCollection<RefundAttemptSnapshot> ?? attempts.ToArray();
        var existing = all.Select(x => x.RefundAttemptId).ToHashSet();
        var reviewable = all
            .Where(x => x.Status == Domain.ValueObjects.RefundAttemptStatus.Failed)
            .Where(x => !existing.Contains(DeriveAttemptId(returnRequestId, x.RefundAttemptId)))
            .Select(x => x.RefundAttemptId)
            .Distinct()
            .ToArray();
        return reviewable.Length == 1 ? reviewable[0] : null;
    }
}
