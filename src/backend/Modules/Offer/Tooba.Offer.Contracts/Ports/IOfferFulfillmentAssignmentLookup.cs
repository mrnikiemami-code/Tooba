using Tooba.Offer.Contracts.Dtos;

namespace Tooba.Offer.Contracts.Ports;

/// <summary>
/// Read-only BATCH port of Offer for fulfillment assignments.
/// </summary>
/// <remarks>
/// The read is batched by the distinct real offer ids of the cart so a consumer never issues one round trip per
/// line. A missing id simply does not appear in the answer: the consumer must refuse an unknown offer instead of
/// inventing a default responsibility.
/// </remarks>
public interface IOfferFulfillmentAssignmentLookup
{
    /// <summary>Assignments of the requested ids, keyed by offer id.</summary>
    /// <param name="offerIds">Distinct offer identities.</param>
    /// <param name="cancellationToken">Caller token.</param>
    Task<IReadOnlyDictionary<Guid, OfferFulfillmentAssignmentSnapshot>> GetAssignmentsAsync(
        IReadOnlyCollection<Guid> offerIds,
        CancellationToken cancellationToken);
}
