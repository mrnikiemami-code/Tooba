namespace Tooba.Offer.Contracts.Dtos;

/// <summary>
/// Fulfillment responsibility of one offer; the two real families confirmed by the product policy.
/// </summary>
/// <remarks>
/// The value is never inferred from the seller id, sales channel, weight or method name. An offer without a
/// value stays UNASSIGNED and a consumer must report unavailability instead of assuming a default.
/// </remarks>
public enum OfferFulfillmentResponsibility
{
    /// <summary>Delivery is the responsibility of the offer's own real seller.</summary>
    Seller = 0,

    /// <summary>Delivery is the platform's responsibility in the trusted edition/tenant context.</summary>
    Platform = 1,
}

/// <summary>
/// Explicit fulfillment assignment of one offer together with the real seller of that same offer.
/// </summary>
/// <remarks>
/// This read-only snapshot exposes no EF type, DbContext, type code or label copy: only the stable type id is
/// carried. <see cref="SellerPartyId"/> is the offer owner and is NOT a responsibility statement.
/// </remarks>
/// <param name="OfferId">Offer identity.</param>
/// <param name="SellerPartyId">Real seller of the same offer.</param>
/// <param name="Responsibility">Explicit responsibility, or <c>null</c> when never assigned.</param>
/// <param name="ShippingTypeId">Real Fulfillment shipping type id, or <c>null</c>.</param>
public sealed record OfferFulfillmentAssignmentSnapshot(
    Guid OfferId,
    Guid SellerPartyId,
    OfferFulfillmentResponsibility? Responsibility,
    Guid? ShippingTypeId);
