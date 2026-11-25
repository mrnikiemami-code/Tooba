using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;

namespace Tooba.Offer.Tests;

/// <summary>Empty batch fulfillment assignment lookup for Offer unit/observability fixtures.</summary>
public sealed class TestFulfillmentAssignments : IOfferFulfillmentAssignmentLookup
{
    /// <inheritdoc />
    public Task<IReadOnlyDictionary<Guid, OfferFulfillmentAssignmentSnapshot>> GetAssignmentsAsync(
        IReadOnlyCollection<Guid> offerIds,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<Guid, OfferFulfillmentAssignmentSnapshot>>(
            new Dictionary<Guid, OfferFulfillmentAssignmentSnapshot>());
}
