namespace Tooba.Settlement.Contracts.Operations;

/// <summary>
/// Posted settlement ledger row for admin order detail financial projection.
/// Amounts are order-attributed only (never batch payout totals).
/// </summary>
public sealed record SettlementAdminOrderEntrySnapshot(
    Guid EntryId,
    Guid SellerOrderId,
    string EntryType,
    decimal GrossAmount,
    decimal CommissionAmount,
    decimal NetAmount,
    string Currency,
    string SourceType,
    DateTimeOffset PostedAt);

/// <summary>
/// Settlement entries required by admin order detail, without Settlement Application/Domain types.
/// </summary>
public interface ISettlementAdminOrderDetailReader
{
    /// <summary>Posted entries keyed by seller order id.</summary>
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<SettlementAdminOrderEntrySnapshot>>> ListEntriesBySellerOrderIdsAsync(
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken);
}
