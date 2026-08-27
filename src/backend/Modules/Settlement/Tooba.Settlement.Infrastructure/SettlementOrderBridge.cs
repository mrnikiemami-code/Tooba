using Tooba.Order.Application;
using Tooba.Settlement.Application;

namespace Tooba.Settlement.Infrastructure;

/// <summary>
/// پل snapshot سفارش برای Settlement از درز Order.Application.
/// </summary>
public sealed class SettlementOrderBridge : ISettlementOrderReader
{
    private readonly IOrderReturnReader _orders;

    /// <summary>پل را به reader سفارش وصل می‌کند.</summary>
    public SettlementOrderBridge(IOrderReturnReader orders) => _orders = orders;

    /// <inheritdoc />
    public async Task<SettlementOrderSnapshot?> GetAsync(Guid sellerOrderId, CancellationToken cancellationToken)
    {
        var context = await _orders.GetReturnContextAsync(sellerOrderId, cancellationToken);
        return context is null
            ? null
            : new SettlementOrderSnapshot(
                context.SellerOrderId,
                context.CheckoutId,
                context.SellerPartyId,
                context.IsPaid,
                context.Currency);
    }
}
