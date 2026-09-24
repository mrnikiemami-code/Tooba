using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;

namespace Tooba.Offer.Application.Policies;

/// <summary>
/// Deterministic primary-offer selection. Preference is sellable-positive units, then the lowest
/// tax-exclusive amount, then OfferId. It is not a front-end guess and never writes price on Product identity.
/// </summary>
public sealed class PrimaryOfferSelectionPolicy : IPrimaryOfferSelectionPolicy
{
    /// <inheritdoc />
    public OfferSelectionCandidate? Resolve(IReadOnlyList<OfferSelectionCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        if (candidates.Count == 0)
        {
            return null;
        }

        return candidates
            .OrderByDescending(candidate => candidate.AvailableUnits > 0)
            .ThenBy(candidate => candidate.AmountExclusiveOfTax)
            .ThenBy(candidate => candidate.OfferId)
            .First();
    }

    /// <inheritdoc />
    public Guid? ResolveVariantId(
        Guid? requestedVariantId,
        IReadOnlyCollection<Guid> productVariantIds,
        IReadOnlyList<OfferSelectionCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(productVariantIds);
        ArgumentNullException.ThrowIfNull(candidates);

        if (requestedVariantId is Guid requested
            && productVariantIds.Contains(requested)
            && candidates.Any(candidate => candidate.CatalogVariantId == requested))
        {
            return requested;
        }

        return Resolve(candidates)?.CatalogVariantId;
    }
}
