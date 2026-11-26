namespace Tooba.Inventory.Contracts.Seller;

/// <summary>Inventory-owned exclusion for a change of an Offer's fulfillment responsibility.</summary>
public interface IOfferFulfillmentChangeGate
{
    /// <summary>
    /// Returns a read-only exclusion lease if Inventory can prove that no protected hold or allocation
    /// prevents a responsibility change. Null is a typed denial at the caller. The caller holds the
    /// lease until its separate Offer-local write commits; disposing it releases the exclusion.
    /// </summary>
    Task<IAsyncDisposable?> TryAcquireAsync(Guid offerId, CancellationToken cancellationToken);
}
