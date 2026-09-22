using Tooba.Settlement.Application.Ports;
using Tooba.Settlement.Contracts.Operations;

namespace Tooba.Settlement.Infrastructure.Adapters;

/// <summary>
/// Exposes the owning Settlement directory as the contracts-only admin order detail reader.
/// </summary>
internal sealed class SettlementAdminOrderDetailReader(ISettlementDirectory directory)
    : ISettlementAdminOrderDetailReader
{
    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<SettlementAdminOrderEntrySnapshot>>> ListEntriesBySellerOrderIdsAsync(
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sellerOrderIds);
        if (sellerOrderIds.Count == 0)
        {
            return new Dictionary<Guid, IReadOnlyList<SettlementAdminOrderEntrySnapshot>>();
        }

        var byOrder = await directory.ListEntriesBySellerOrderIdsAsync(sellerOrderIds, cancellationToken);
        return byOrder.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<SettlementAdminOrderEntrySnapshot>)pair.Value
                .Select(entry => new SettlementAdminOrderEntrySnapshot(
                    entry.EntryId,
                    pair.Key,
                    entry.EntryType.ToString(),
                    entry.GrossAmount,
                    entry.CommissionAmount,
                    entry.NetAmount,
                    entry.Currency,
                    entry.SourceType,
                    entry.PostedAt))
                .ToArray());
    }
}
