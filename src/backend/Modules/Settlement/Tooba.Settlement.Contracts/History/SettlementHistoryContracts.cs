namespace Tooba.Settlement.Contracts.History;

/// <summary>
/// One posted settlement ledger row attributed to a seller order. Carries no commission money.
/// </summary>
/// <param name="SellerOrderId">Seller order the row is attributed to.</param>
/// <param name="SourceType">Stable source discriminator such as <c>order_cancel</c> or <c>refund</c>.</param>
/// <param name="EntryType">Stable entry direction, <c>Credit</c> or <c>Debit</c>.</param>
/// <param name="PostedAt">Instant the row was posted.</param>
public sealed record SettlementHistoryEntry(
    Guid SellerOrderId,
    string SourceType,
    string EntryType,
    DateTimeOffset PostedAt);

/// <summary>Read-only Settlement history for seller orders, consumed by admin operational timelines.</summary>
public interface ISettlementHistoryReader
{
    /// <summary>Posted settlement rows for the given seller orders.</summary>
    Task<IReadOnlyList<SettlementHistoryEntry>> ListBySellerOrderIdsAsync(
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken);
}

/// <summary>Stable settlement source type strings exposed through the history contract.</summary>
public static class SettlementHistorySourceTypes
{
    /// <summary>Seller share adjustment caused by an order cancellation.</summary>
    public const string OrderCancel = "order_cancel";

    /// <summary>Seller share adjustment caused by a refund.</summary>
    public const string Refund = "refund";
}

/// <summary>Stable settlement entry type strings exposed through the history contract.</summary>
public static class SettlementHistoryEntryTypes
{
    /// <summary>Credit toward the seller share.</summary>
    public const string Credit = "Credit";

    /// <summary>Debit against the seller share.</summary>
    public const string Debit = "Debit";
}
