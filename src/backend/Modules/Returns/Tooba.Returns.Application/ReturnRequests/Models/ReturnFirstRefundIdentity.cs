using System.Security.Cryptography;
using System.Text;

namespace Tooba.Returns.Application.ReturnRequests.Models;

/// <summary>
/// TB-277: the ONE deterministic identity rule of the FIRST refund of one approved case.
/// <para>
/// Approval is a decision; the first money execution is its own deliberate intent. This case can only ever own ONE
/// first attempt, so its identity is derived from the case id alone (never a counter, a timestamp or a random key).
/// A duplicate click, a lost response or a remount therefore resolves to the SAME attempt row and the SAME
/// provider-facing idempotency key: the primary key and the existing unique key arbitrate, and a second first
/// attempt cannot exist.
/// </para>
/// <para>
/// The provider key keeps the shape the owning approval already used (<c>refund-{case:N}</c>), so a case approved
/// before this separation keeps its recorded key meaning and no historical row is reinterpreted.
/// </para>
/// </summary>
public static class ReturnFirstRefundIdentity
{
    /// <summary>The deterministic attempt id of the first (and only) refund of one case.</summary>
    public static Guid DeriveAttemptId(Guid returnRequestId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"tb277-refund-first:{returnRequestId:N}"));
        return new Guid(hash.AsSpan(0, 16));
    }

    /// <summary>The deterministic provider-facing idempotency key of that same first attempt.</summary>
    public static string DeriveIdempotencyKey(Guid returnRequestId) => $"refund-{returnRequestId:N}";
}
