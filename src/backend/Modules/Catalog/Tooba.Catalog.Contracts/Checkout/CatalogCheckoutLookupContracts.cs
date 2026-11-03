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
    /// Resolved by Catalog physical-specification authority: typed <c>WeightGrams</c> composed with
    /// legacy active Number <c>weight</c>/<c>*_weight</c> attributes (g/kg) under an explicit
    /// conflict policy (agree / typed-only / legacy-only / fail closed). Variant typed null inherits
    /// product; present typed variant replaces product. Missing, conflicting, non-positive, or
    /// overflow yields a missing map entry. No invented defaults. Dimensions are never summed.
    /// </remarks>
    Task<IReadOnlyDictionary<Guid, long?>> GetShippingWeightGramsByVariantIdsAsync(
        IReadOnlyCollection<Guid> variantIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// Neutral product physical facts (grams + centimetres) keyed by variant id for Order/Shipping.
    /// </summary>
    /// <remarks>
    /// Catalog-owned only — no foreign persistence. Null fields mean unspecified. Product dimensions
    /// are per-item facts and must not be summed into parcels; parcel L/W/H remain separate packing
    /// facts. Method binding default package dimensions must not masquerade as these values.
    /// </remarks>
    Task<IReadOnlyDictionary<Guid, CatalogPhysicalSpecificationFacts>> GetPhysicalSpecificationsByVariantIdsAsync(
        IReadOnlyCollection<Guid> variantIds,
        CancellationToken cancellationToken);
}

/// <summary>
/// Catalog-owned physical specification facts for one sellable variant (product + lawful override).
/// </summary>
/// <param name="WeightGrams">Whole grams; null when unspecified or fail-closed.</param>
/// <param name="LengthCentimeters">Length in centimetres; null when unspecified.</param>
/// <param name="WidthCentimeters">Width in centimetres; null when unspecified.</param>
/// <param name="HeightCentimeters">Height in centimetres; null when unspecified.</param>
public sealed record CatalogPhysicalSpecificationFacts(
    long? WeightGrams,
    decimal? LengthCentimeters,
    decimal? WidthCentimeters,
    decimal? HeightCentimeters);
