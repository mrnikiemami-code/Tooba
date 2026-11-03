namespace Tooba.Catalog.Domain.Rules;

/// <summary>
/// Catalog-owned physical product specification authority (grams + centimetres).
/// </summary>
/// <remarks>
/// <para>
/// Typed fields (<c>WeightGrams</c>, <c>LengthCentimeters</c>, <c>WidthCentimeters</c>,
/// <c>HeightCentimeters</c>) are the explicit write/read contract. Null means unspecified —
/// never fabricate 0. Provided values must be positive with bounded precision/overflow.
/// </para>
/// <para>
/// <b>Weight compatibility with legacy attributes:</b> active Number
/// <c>weight</c>/<c>*_weight</c> attributes (g/kg) remain readable provenance via
/// <see cref="CatalogShippingWeightGrams"/>. Resolution policy:
/// </para>
/// <list type="bullet">
/// <item>typed only → typed (whole grams)</item>
/// <item>legacy only → legacy grams</item>
/// <item>both present and equal → agreed grams</item>
/// <item>both present and unequal → unknown (fail closed; no silent preference)</item>
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

    /// <summary>Max fractional digits for weight grams.</summary>
    public const int WeightGramsScale = 3;

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
        ValidateOptionalPositive(
            weightGrams,
            WeightGramsMaxValue,
            WeightGramsScale,
            "catalog.physical.weight_grams.invalid");
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
    /// Compose typed whole grams with legacy attribute grams under the explicit conflict policy.
    /// </summary>
    public static long? ResolveEffectiveWeightGrams(long? typedWholeGrams, long? legacyAttributeGrams)
    {
        if (typedWholeGrams is null && legacyAttributeGrams is null)
        {
            return null;
        }

        if (typedWholeGrams is null)
        {
            return legacyAttributeGrams;
        }

        if (legacyAttributeGrams is null)
        {
            return typedWholeGrams;
        }

        return typedWholeGrams.Value == legacyAttributeGrams.Value
            ? typedWholeGrams
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
        var weight = ResolveEffectiveWeightGrams(TryToWholeGrams(typedWeight), legacyAttributeGrams);
        return (
            weight,
            ResolveTypedDimensionCentimeters(productLengthCentimeters, variantLengthCentimeters),
            ResolveTypedDimensionCentimeters(productWidthCentimeters, variantWidthCentimeters),
            ResolveTypedDimensionCentimeters(productHeightCentimeters, variantHeightCentimeters));
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
