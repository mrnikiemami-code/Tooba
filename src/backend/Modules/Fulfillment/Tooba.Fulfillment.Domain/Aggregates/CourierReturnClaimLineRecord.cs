using Tooba.Fulfillment.Domain.ValueObjects;

namespace Tooba.Fulfillment.Domain.Aggregates;

/// <summary>
/// TB-265-R1: the per-line row of one courier-origin source claim.
/// <para>
/// The whole claim payload is still the durable replay identity, but source-quantity exclusivity is per LINE: this row
/// carries its own <see cref="ActiveKey"/>, so the database (a unique index on the key) — not a caller's aggregate
/// list — is what makes two claims of the same (receipt, line, condition) quantity impossible. A terminal claim clears
/// its line keys, so history stays readable while new lawful claims become possible again.
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
    /// Stable per-line capacity key. It does NOT contain the claim id: two active claims of the same physical
    /// (receipt, line, condition) quantity collide on the unique index, which is the actual exclusivity authority.
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
