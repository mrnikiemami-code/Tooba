using Tooba.Settlement.Application.Ports;
using Tooba.Settlement.Contracts.History;

namespace Tooba.Settlement.Infrastructure.Adapters;

/// <summary>
/// Exposes the owning Settlement directory as the contracts-only history reader.
/// </summary>
internal sealed class SettlementHistoryReader(ISettlementDirectory directory) : ISettlementHistoryReader
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SettlementHistoryEntry>> ListBySellerOrderIdsAsync(
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sellerOrderIds);
        if (sellerOrderIds.Count == 0)
        {
            return [];
        }

        var byOrder = await directory.ListEntriesBySellerOrderIdsAsync(sellerOrderIds, cancellationToken);
        return byOrder
            .SelectMany(pair => pair.Value.Select(entry => new SettlementHistoryEntry(
                pair.Key,
                entry.SourceType,
                entry.EntryType.ToString(),
                entry.PostedAt)))
            .ToList();
    }
}
