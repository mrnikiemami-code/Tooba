using Tooba.Order.Contracts.Returns;

namespace Tooba.Order.Infrastructure.Integrations.Returns;

/// <summary>
/// TB-265: Order-owned authority over the remaining customer delivery obligation of one line when a courier-origin
/// Returns case is approved.
/// <para>
/// It authors the decision and identifies the exact held reservation of that line; it NEVER decrements ordered,
/// packed or shipped history (a returned courier parcel is not an "unshipped" parcel) and it never releases stock
/// itself. The caller closes the branch and releases only the unconsumed held quantity once, through the owning
/// Inventory seam.
/// </para>
/// </summary>
public sealed class OrderReturnObligationClosure : IOrderReturnObligationClosure
{
    private readonly IOrderReturnReader _orders;

    /// <summary>Wires the closure to the Order-owned return snapshot read model.</summary>
    public OrderReturnObligationClosure(IOrderReturnReader orders) => _orders = orders;

    /// <inheritdoc />
    public async Task<OrderLineObligationClosureResult> CloseLineObligationAsync(
        OrderLineObligationClosureRequest request,
        CancellationToken cancellationToken)
    {
        if (request.SellerOrderId == Guid.Empty || request.OrderLineId == Guid.Empty)
        {
            return Refused(request.OrderLineId, "order.return.source_required");
        }

        if (request.Quantity <= 0m)
        {
            return Refused(request.OrderLineId, "order.return.quantity_invalid");
        }

        var context = await _orders.GetReturnContextAsync(request.SellerOrderId, cancellationToken);
        if (context is null)
        {
            return Refused(request.OrderLineId, "return.missing");
        }

        if (context.CheckoutId != request.CheckoutId)
        {
            return Refused(request.OrderLineId, "order.return.checkout_mismatch");
        }

        if (!context.IsPaid)
        {
            // A never-paid order has no money to refund and no delivered obligation to close.
            return Refused(request.OrderLineId, "return.not_paid");
        }

        var line = context.Lines.SingleOrDefault(x => x.OrderLineId == request.OrderLineId);
        if (line is null)
        {
            return Refused(request.OrderLineId, "return.line_missing");
        }

        if (request.Quantity > line.Quantity)
        {
            return Refused(request.OrderLineId, "return.quantity_exceeded");
        }

        var remaining = line.Quantity - request.Quantity;
        return new OrderLineObligationClosureResult(
            true,
            request.OrderLineId,
            request.Quantity,
            remaining > 0m ? remaining : 0m,
            line.ReservationId);
    }

    private static OrderLineObligationClosureResult Refused(Guid orderLineId, string blockerCode) =>
        new(false, orderLineId, 0m, 0m, null, blockerCode);
}
