using Tooba.Returns.Application.Models;
using Tooba.Returns.Application.Ports;
using Tooba.Returns.Contracts.History;

namespace Tooba.Returns.Infrastructure.Adapters;

/// <summary>
/// Exposes the owning Returns directory as the contracts-only history reader.
/// </summary>
internal sealed class ReturnHistoryReader(IReturnDirectory directory) : IReturnHistoryReader
{
    private const int MaxRecords = 200;

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReturnHistoryRecord>> ListBySellerOrderIdsAsync(
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sellerOrderIds);
        if (sellerOrderIds.Count == 0)
        {
            return [];
        }

        var returns = await directory.ListBySellerOrderIdsAsync(sellerOrderIds, cancellationToken);
        return returns
            .OrderByDescending(x => x.CreatedAt)
            .Take(MaxRecords)
            .Select(Map)
            .ToList();
    }

    private static ReturnHistoryRecord Map(ReturnSnapshot snapshot) =>
        new(
            snapshot.ReturnRequestId,
            snapshot.SellerOrderId,
            snapshot.RequestedByUserId,
            snapshot.Status.ToString(),
            snapshot.CreatedAt,
            snapshot.UpdatedAt,
            snapshot.Items.Select(x => new ReturnHistoryItem(x.OrderLineId, x.Quantity)).ToList(),
            snapshot.RefundAttempts
                .Select(x => new ReturnHistoryRefundAttempt(
                    x.Status.ToString(),
                    x.Amount,
                    x.Currency,
                    x.CreatedAt,
                    x.CompletedAt))
                .ToList());
}
