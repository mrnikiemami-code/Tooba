namespace Tooba.Order.Contracts.Fulfillment;

/// <summary>Order-owned evidence required before changing a listing's fulfillment responsibility.</summary>
public interface IOrderOfferCommitmentReader
{
    /// <summary>
    /// True when an order line still carries the Offer identity. Existing order states can be paid,
    /// restored from cancellation or used for after-sale recovery; absence of a current inventory
    /// hold alone does not prove absence of these obligations.
    /// </summary>
    Task<bool> HasOrderCommitmentAsync(Guid offerId, CancellationToken cancellationToken);
}
