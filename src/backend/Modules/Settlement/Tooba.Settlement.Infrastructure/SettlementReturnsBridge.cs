using Tooba.Returns.Application;
using Tooba.Settlement.Application;

namespace Tooba.Settlement.Infrastructure;

/// <summary>
/// پل snapshot refund برای Settlement از درز Returns.Application.
/// </summary>
public sealed class SettlementReturnsBridge : ISettlementReturnsReader
{
    private readonly IReturnSettlementReader _returns;

    /// <summary>پل را به reader refund وصل می‌کند.</summary>
    public SettlementReturnsBridge(IReturnSettlementReader returns) => _returns = returns;

    /// <inheritdoc />
    public async Task<SettlementRefundSnapshot?> GetAsync(Guid returnRequestId, CancellationToken cancellationToken)
    {
        var snapshot = await _returns.GetAsync(returnRequestId, cancellationToken);
        return snapshot is null
            ? null
            : new SettlementRefundSnapshot(
                snapshot.ReturnRequestId,
                snapshot.SellerOrderId,
                snapshot.SellerPartyId,
                snapshot.RefundAmount,
                snapshot.Currency);
    }
}
