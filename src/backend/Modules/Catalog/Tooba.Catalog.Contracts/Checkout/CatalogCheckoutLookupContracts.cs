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
    /// Sourced only from <b>active</b> Number definitions whose machine Code is the established
    /// shipping/product weight identity (<c>weight</c> or <c>*_weight</c>); unit <c>g</c>/<c>kg</c>
    /// is conversion metadata only. Variant value overrides the same product definition when
    /// present; a present invalid/conflicting override stays unknown (never healed from product).
    /// Missing, unparsable, non-positive, overflow, or conflicting effective grams yield a missing
    /// map entry. Cancellation/provider failure propagates. No invented defaults.
    /// </remarks>
    Task<IReadOnlyDictionary<Guid, long?>> GetShippingWeightGramsByVariantIdsAsync(
        IReadOnlyCollection<Guid> variantIds,
        CancellationToken cancellationToken);
}
