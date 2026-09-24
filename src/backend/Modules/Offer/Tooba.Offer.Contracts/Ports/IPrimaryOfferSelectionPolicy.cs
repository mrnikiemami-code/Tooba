using Tooba.Offer.Contracts.Dtos;

namespace Tooba.Offer.Contracts.Ports;

/// <summary>
/// Offer-owned primary-offer selection boundary. Callers pass already-composed candidates whose
/// amount came from Pricing and whose availability came from Inventory; the policy only ranks.
/// </summary>
public interface IPrimaryOfferSelectionPolicy
{
    /// <summary>
    /// Selects the primary offer from pre-composed candidates, or <see langword="null"/> when none is available.
    /// </summary>
    OfferSelectionCandidate? Resolve(IReadOnlyList<OfferSelectionCandidate> candidates);

    /// <summary>
    /// Keeps the requested variant only when it belongs to the product and has an available candidate;
    /// otherwise falls back to the selected primary offer's variant.
    /// </summary>
    Guid? ResolveVariantId(
        Guid? requestedVariantId,
        IReadOnlyCollection<Guid> productVariantIds,
        IReadOnlyList<OfferSelectionCandidate> candidates);
}
