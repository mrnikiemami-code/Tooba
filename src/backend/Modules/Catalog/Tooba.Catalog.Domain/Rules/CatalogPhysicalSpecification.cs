namespace Tooba.Catalog.Domain.Rules;

/// <summary>
/// Catalog-owned physical product specification authority (grams + centimetres).
/// </summary>
/// <remarks>
/// <para>
/// Typed fields (<c>WeightGrams</c>, <c>LengthCentimeters</c>, <c>WidthCentimeters</c>,
/// <c>HeightCentimeters</c>) are the explicit write/read contract. Null means unspecified —
/// never fabricate 0. Provided values must be positive with bounded precision/overflow.
/// Typed weight writes require whole grams (no fractional grams). Centimetre dimensions
/// retain bounded fractional scale.
/// </para>
/// <para>
/// <b>Weight compatibility with legacy attributes:</b> active Number
/// <c>weight</c>/<c>*_weight</c> attributes (g/kg) remain readable provenance via
/// <see cref="CatalogShippingWeightGrams"/>. Resolution policy:
/// </para>
/// <list type="bullet">
/// <item>typed absent (null) + legacy → legacy grams</item>
/// <item>typed present and whole → typed; equal legacy agrees; unequal legacy → unknown</item>
/// <item>typed present but not whole/usable → unknown (fail closed; NEVER fall through to legacy)</item>
/// <item>neither → unknown</item>
/// </list>
/// <para>
/// Variant typed null inherits product typed. A present typed variant value replaces product
/// (including when it would leave weight unknown after whole-gram normalization). Dimensions
/// have no legacy attribute authority — method/package defaults must never masquerade as
/// product dimensions. Product dimensions of different items are never summed here.
/// </para>
/// </remarks>
public static class CatalogPhysicalSpecification
{
    /// <summary>Upper bound for typed weight in grams (aligns with shipping rate weight max).</summary>
    public const decimal WeightGramsMaxValue = 1_000_000_000m;

    /// <summary>Upper bound for each dimension in centimetres.</summary>
    public const decimal DimensionCentimetersMaxValue = 1_000_000m;

    /// <summary>
    /// Typed weight fractional digits allowed on write: whole grams only.
    /// Validation uses truncate-equality (so <c>500.0</c> is accepted; <c>500.5</c> is not).
    /// </summary>
    public const int WeightGramsScale = 0;

    /// <summary>Max fractional digits for length/width/height centimetres.</summary>
    public const int DimensionCentimetersScale = 2;

    /// <summary>
    /// Validate optional typed physical fields. Nulls are allowed (unspecified).
    /// Throws <see cref="InvalidOperationException"/> with a stable machine code on reject.
    /// </summary>
    public static void ValidateOrThrow(
        decimal? weightGrams,
        decimal? lengthCentimeters,
        decimal? widthCentimeters,
        decimal? heightCentimeters)
    {
        ValidateOptionalWholeGrams(weightGrams);
        ValidateOptionalPositive(
            lengthCentimeters,
            DimensionCentimetersMaxValue,
            DimensionCentimetersScale,
            "catalog.physical.length_cm.invalid");
        ValidateOptionalPositive(
            widthCentimeters,
            DimensionCentimetersMaxValue,
            DimensionCentimetersScale,
            "catalog.physical.width_cm.invalid");
        ValidateOptionalPositive(
            heightCentimeters,
            DimensionCentimetersMaxValue,
            DimensionCentimetersScale,
            "catalog.physical.height_cm.invalid");
    }

    /// <summary>
    /// Effective typed weight: present variant replaces product; absent variant inherits.
    /// </summary>
    public static decimal? ResolveTypedWeightGrams(decimal? productWeightGrams, decimal? variantWeightGrams)
        => variantWeightGrams ?? productWeightGrams;

    /// <summary>
    /// Effective typed dimension: present variant replaces product; absent variant inherits.
    /// </summary>
    public static decimal? ResolveTypedDimensionCentimeters(
        decimal? productCentimeters,
        decimal? variantCentimeters)
        => variantCentimeters ?? productCentimeters;

    /// <summary>
    /// Normalize typed weight to whole grams for shipping/order consumption.
    /// Fractional or out-of-range → null (never truncate silently).
    /// </summary>
    public static long? TryToWholeGrams(decimal? weightGrams)
    {
        if (weightGrams is null)
        {
            return null;
        }

        var value = weightGrams.Value;
        if (value <= 0m || value > WeightGramsMaxValue)
        {
            return null;
        }

        if (value != decimal.Truncate(value) || value > long.MaxValue)
        {
            return null;
        }

        return (long)value;
    }

    /// <summary>
    /// Compose typed weight with legacy attribute grams under the explicit conflict policy.
    /// Legacy applies only when typed is absent (null). A present but unusable typed value
    /// fails closed and never falls through to legacy.
    /// </summary>
    public static long? ResolveEffectiveWeightGrams(decimal? typedWeightGrams, long? legacyAttributeGrams)
    {
        if (typedWeightGrams is null)
        {
            return legacyAttributeGrams;
        }

        // Present typed: usable whole grams required; never heal from legacy.
        var typedWhole = TryToWholeGrams(typedWeightGrams);
        if (typedWhole is null)
        {
            return null;
        }

        if (legacyAttributeGrams is null)
        {
            return typedWhole;
        }

        return typedWhole.Value == legacyAttributeGrams.Value
            ? typedWhole
            : null;
    }

    /// <summary>
    /// Resolve effective product/variant physical facts for one variant.
    /// </summary>
    public static (
        long? WeightGrams,
        decimal? LengthCentimeters,
        decimal? WidthCentimeters,
        decimal? HeightCentimeters)
        ResolveEffectiveFacts(
            decimal? productWeightGrams,
            decimal? productLengthCentimeters,
            decimal? productWidthCentimeters,
            decimal? productHeightCentimeters,
            decimal? variantWeightGrams,
            decimal? variantLengthCentimeters,
            decimal? variantWidthCentimeters,
            decimal? variantHeightCentimeters,
            long? legacyAttributeGrams)
    {
        var typedWeight = ResolveTypedWeightGrams(productWeightGrams, variantWeightGrams);
        var weight = ResolveEffectiveWeightGrams(typedWeight, legacyAttributeGrams);
        return (
            weight,
            ResolveTypedDimensionCentimeters(productLengthCentimeters, variantLengthCentimeters),
            ResolveTypedDimensionCentimeters(productWidthCentimeters, variantWidthCentimeters),
            ResolveTypedDimensionCentimeters(productHeightCentimeters, variantHeightCentimeters));
    }

    private static void ValidateOptionalWholeGrams(decimal? weightGrams)
    {
        if (weightGrams is null)
        {
            return;
        }

        var amount = weightGrams.Value;
        if (amount <= 0m || amount > WeightGramsMaxValue)
        {
            throw new InvalidOperationException("catalog.physical.weight_grams.invalid");
        }

        // Whole grams only — do not use decimal scale bits (500.0 must remain valid).
        if (amount != decimal.Truncate(amount) || amount > long.MaxValue)
        {
            throw new InvalidOperationException("catalog.physical.weight_grams.invalid");
        }
    }

    private static void ValidateOptionalPositive(
        decimal? value,
        decimal maxInclusive,
        int maxScale,
        string errorCode)
    {
        if (value is null)
        {
            return;
        }

        var amount = value.Value;
        if (amount <= 0m || amount > maxInclusive)
        {
            throw new InvalidOperationException(errorCode);
        }

        if (GetScale(amount) > maxScale)
        {
            throw new InvalidOperationException(errorCode);
        }
    }

    private static int GetScale(decimal value)
    {
        return (decimal.GetBits(value)[3] >> 16) & 0xFF;
    }
}
