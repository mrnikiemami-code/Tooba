using Tooba.Fulfillment.Domain.ValueObjects;

namespace Tooba.Fulfillment.Domain.Aggregates;

/// <summary>
/// TB-265-R1: the per-line row of one courier-origin source claim.
/// <para>
/// The whole claim payload is still the durable replay identity. This row records the exact claimed quantity per line
/// and carries a per-line capacity key for lookup. TB-265-R2: that key is deliberately NOT unique — two lawful claims
/// of disjoint quantities on the same (receipt, line, condition) must both be accepted, so uniqueness would advertise
/// a remaining quantity and then refuse it. Overlap is prevented by the owning serialized capacity authority (the
/// Serializable transaction, the Fulfillment row lock and the aggregate's free-quantity computation), not by this key.
/// A terminal claim clears its keys so history stays readable while nothing blocks a new lawful claim.
/// </para>
/// </summary>
public sealed class CourierReturnClaimLineRecord
{
    private CourierReturnClaimLineRecord()
    {
    }

    /// <summary>Stable identity of this claimed line.</summary>
    public Guid ClaimLineId { get; init; }

    /// <summary>Owning claim.</summary>
    public Guid ClaimId { get; init; }

    /// <summary>Order line whose received quantity is claimed.</summary>
    public Guid OrderLineId { get; init; }

    /// <summary>Physical condition of the claimed quantity.</summary>
    public CourierReturnClaimCondition Condition { get; init; }

    /// <summary>Claimed quantity.</summary>
    public decimal Quantity { get; init; }

    /// <summary>Per-line exclusivity guard; non-null only while the claim still holds this capacity.</summary>
    public string? ActiveKey { get; private set; }

    /// <summary>When the line was claimed.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>Last change.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Creates the durable line row of a claim.</summary>
    public static CourierReturnClaimLineRecord Create(
        Guid claimLineId,
        Guid claimId,
        Guid shipmentReturnReceiptId,
        Guid orderLineId,
        CourierReturnClaimCondition condition,
        decimal quantity,
        DateTimeOffset now) =>
        new()
        {
            ClaimLineId = claimLineId,
            ClaimId = claimId,
            OrderLineId = orderLineId,
            Condition = condition,
            Quantity = quantity,
            ActiveKey = BuildActiveKey(shipmentReturnReceiptId, orderLineId, condition),
            CreatedAt = now,
            UpdatedAt = now,
        };

    /// <summary>
    /// Stable per-line capacity key (lookup only, never an overlap guard). It does NOT contain the claim id: the key
    /// identifies the physical (receipt, line, condition) pool this line drew from, and the owning serialized
    /// authority — not a unique index — is what prevents two claims from overdrawing that pool.
    /// </summary>
    public static string BuildActiveKey(
        Guid shipmentReturnReceiptId,
        Guid orderLineId,
        CourierReturnClaimCondition condition) =>
        $"{shipmentReturnReceiptId:N}:{orderLineId:N}:{(int)condition}";

    /// <summary>Releases this line's capacity (terminal claim). Idempotent.</summary>
    public void ReleaseCapacity(DateTimeOffset now)
    {
        if (ActiveKey is null)
        {
            UpdatedAt = now;
            return;
        }

        ActiveKey = null;
        UpdatedAt = now;
    }
}
