namespace Tooba.Offer.Domain.ValueObjects;

/// <summary>
/// Delivery responsibility of one listing; the only two real families are Seller and Platform.
/// </summary>
/// <remarks>
/// The value is explicit. It is never inferred from the seller id, display name, method code or an absent legacy
/// field. A listing without a value stays unassigned and the customer projection reports it as unavailable.
/// </remarks>
public enum FulfillmentResponsibility
{
    /// <summary>Delivery is the responsibility of this listing's own real seller.</summary>
    Seller = 0,

    /// <summary>Delivery is the platform's responsibility: Store in SingleStore, Marketplace in Marketplace.</summary>
    Platform = 1,
}
