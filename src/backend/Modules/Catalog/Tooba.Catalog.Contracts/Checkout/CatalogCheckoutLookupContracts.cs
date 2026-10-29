using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Contracts.Checkout;

/// <summary>
/// Checkout-facing Catalog lookup without Catalog.Application or DbContext leakage.
/// Owned by Catalog; consumed by Order checkout orchestration.
/// </summary>
public interface ICatalogCheckoutLookup
{
    /// <summary>Primary category ids for variants (seller auth / catalog snapshot fallback).</summary>
    Task<IReadOnlyDictionary<Guid, Guid?>> GetPrimaryCategoryIdsByVariantIdsAsync(
        IReadOnlyCollection<Guid> variantIds,
        CancellationToken cancellationToken);

    /// <summary>Effective quantity policies keyed by variant id.</summary>
    Task<IReadOnlyDictionary<Guid, EffectiveQuantityPolicy>> GetEffectiveQuantityPoliciesForVariantIdsAsync(
        IReadOnlyCollection<Guid> variantIds,
        CancellationToken cancellationToken);

    /// <summary>One store-global rounding mode.</summary>
    Task<QuantityRoundingMode> GetGlobalRoundingModeAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Authoritative shipping weight in whole grams keyed by variant id.
    /// </summary>
    /// <remarks>
    /// Sourced only from stored Number attribute values whose definition unit is
    /// <c>g</c> or <c>kg</c> (case-insensitive). Variant-level values override product-level for
    /// the same definition. Missing, unparsable, non-positive, or conflicting converted grams
    /// yield a missing map entry (caller treats as measurement unknown). No invented defaults.
    /// </remarks>
    Task<IReadOnlyDictionary<Guid, long?>> GetShippingWeightGramsByVariantIdsAsync(
        IReadOnlyCollection<Guid> variantIds,
        CancellationToken cancellationToken);
}
