namespace Tooba.Returns.Application.ReturnRequests.Models;

/// <summary>
/// TB-278: ONE customer-selectable order line, as the owning authorities actually answer it.
/// <para>
/// Every fact here is read from an owning module through its Contracts:
/// <list type="bullet">
/// <item><description><see cref="DeliveredQuantity"/>, <see cref="AlreadyReturnedQuantity"/> and
/// <see cref="RemainingReturnableQuantity"/> come from the SAME eligibility evaluator that the create command runs —
/// never from the shipped quantity, the aggregate order status or the browser clock;</description></item>
/// <item><description><see cref="UnitDecimalPlaces"/> and <see cref="UnitLabel"/> are the owning accepted line's unit
/// snapshot (the quantum a lawful quantity must land on), and <c>null</c> stays honestly unknown;</description></item>
/// <item><description><see cref="ProductLabel"/> is the Catalog variant title, so a customer selects a real product
/// instead of a GUID.</description></item>
/// </list>
/// </para>
/// <para>
/// <see cref="BlockerCode"/> is the ONE stable reason this line cannot be selected right now (<c>null</c> when it
/// can). It is a read-side fact; the write path re-evaluates the same authority inside its serialized critical
/// section and stays final.
/// </para>
/// </summary>
public sealed record CustomerReturnEligibilityLine(
    Guid OrderLineId,
    string? ProductLabel,
    string? UnitLabel,
    int? UnitDecimalPlaces,
    decimal DeliveredQuantity,
    decimal AlreadyReturnedQuantity,
    decimal RemainingReturnableQuantity,
    bool IsReturnable,
    int ReturnWindowDays,
    DateTimeOffset? ReturnableUntil,
    string? BlockerCode)
{
    /// <summary>True when this exact line can fund a new customer selection.</summary>
    public bool Selectable => BlockerCode is null && RemainingReturnableQuantity > 0 && UnitDecimalPlaces is not null;
}

/// <summary>
/// TB-278: the authenticated customer's authoritative return eligibility for ONE seller order.
/// <para>
/// The read is bound to the checkout's owning customer BEFORE any fact is exposed, so a foreign seller order can never
/// leak a product, a quantity or a blocker. It carries no permission of its own: the create command re-evaluates the
/// same authority under the owning per-seller-order serialization.
/// </para>
/// </summary>
public sealed record CustomerReturnEligibilitySnapshot(
    Guid SellerOrderId,
    Guid CheckoutId,
    string Currency,
    bool Eligible,
    string ReasonCode,
    DateTimeOffset? EligibleUntil,
    DateTimeOffset? LastDeliveredAt,
    IReadOnlyList<CustomerReturnEligibilityLine> Lines);
