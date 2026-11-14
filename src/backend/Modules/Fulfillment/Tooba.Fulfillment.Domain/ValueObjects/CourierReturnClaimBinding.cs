namespace Tooba.Fulfillment.Domain.ValueObjects;

/// <summary>
/// TB-265-R1: the reduced shape of an ACTIVE courier-origin Returns claim that the owning unit needs in order to
/// stop the very same received quantity from funding a competing send. The claim rows live in their own aggregate
/// (<see cref="Aggregates.CourierReturnClaim"/>); this value object only carries the identity facts the aggregate must
/// enforce — the source parcel/receipt, the exact line/condition/quantity and the claim's durable phase.
/// </summary>
/// <param name="ClaimId">The owning claim.</param>
/// <param name="ShipmentId">The returned source parcel.</param>
/// <param name="ReceiptId">The single origin receipt the quantity came back on.</param>
/// <param name="Approved">True once the case is approved (the branch is final); false while only requested.</param>
/// <param name="OrderLineId">The claimed order line.</param>
/// <param name="Condition">Physical condition of the claimed quantity.</param>
/// <param name="Quantity">Claimed quantity (positive).</param>
public readonly record struct CourierReturnClaimBinding(
    Guid ClaimId,
    Guid ShipmentId,
    Guid ReceiptId,
    bool Approved,
    Guid OrderLineId,
    CourierReturnClaimCondition Condition,
    decimal Quantity);
