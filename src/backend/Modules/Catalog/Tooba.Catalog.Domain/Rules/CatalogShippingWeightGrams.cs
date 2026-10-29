using System.Globalization;

namespace Tooba.Catalog.Domain.Rules;

/// <summary>
/// Established Catalog semantic for total shipping/product weight attributes.
/// </summary>
/// <remarks>
/// Identity is the active Number definition whose machine <c>Code</c> is exactly
/// <c>weight</c> or ends with <c>_weight</c> (demo/seed convention:
/// <c>demo_attr_laptop_weight</c>, <c>demo_attr_mobile_weight</c>, matrix field
/// <c>weight</c>). Display <c>Unit</c> is conversion metadata only (<c>g</c>/<c>kg</c>);
/// unrelated Number+unit attributes (e.g. volume in g) are not shipping weight.
/// </remarks>
public static class CatalogShippingWeightGrams
{
    /// <summary>
    /// Whether this definition is an established active shipping/product weight authority.
    /// </summary>
    public static bool IsShippingWeightDefinition(
        string code,
        CatalogAttributeValueKind valueKind,
        bool isActive,
        string? unit)
    {
        if (!isActive || valueKind != CatalogAttributeValueKind.Number)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(unit))
        {
            return false;
        }

        if (!IsWeightCode(code.Trim()))
        {
            return false;
        }

        var trimmedUnit = unit.Trim();
        return trimmedUnit.Equals("g", StringComparison.OrdinalIgnoreCase)
            || trimmedUnit.Equals("kg", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Stable machine-code identity for shipping weight (never translated label).</summary>
    public static bool IsWeightCode(string code)
    {
        if (code.Equals("weight", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return code.EndsWith("_weight", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Parse a stored canonical Number and convert to whole grams. Malformed/overflow/fractional/non-positive → null.
    /// </summary>
    public static long? TryConvertCanonicalToGrams(string? canonicalValue, string unit)
    {
        if (string.IsNullOrWhiteSpace(canonicalValue) || string.IsNullOrWhiteSpace(unit))
        {
            return null;
        }

        // Canonical Number syntax only: optional leading sign + digits + optional fraction.
        // Do NOT use NumberStyles.Number (thousands separators would silently accept "1,4").
        const NumberStyles canonicalNumber =
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;
        if (!decimal.TryParse(
                canonicalValue.Trim(),
                canonicalNumber,
                CultureInfo.InvariantCulture,
                out var amount)
            || amount <= 0m)
        {
            return null;
        }

        decimal grams;
        if (unit.Trim().Equals("kg", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                grams = checked(amount * 1000m);
            }
            catch (OverflowException)
            {
                return null;
            }
        }
        else if (unit.Trim().Equals("g", StringComparison.OrdinalIgnoreCase))
        {
            grams = amount;
        }
        else
        {
            return null;
        }

        if (grams != decimal.Truncate(grams) || grams > long.MaxValue || grams <= 0m)
        {
            return null;
        }

        return (long)grams;
    }

    /// <summary>
    /// Build effective grams: variant override replaces the SAME definition when present;
    /// a PRESENT invalid override makes that definition unknown (never healed from product);
    /// absent override may inherit product. Conflicting effective definitions → unknown.
    /// </summary>
    public static long? ResolveEffectiveGrams(
        IReadOnlyDictionary<Guid, string> unitByDefinition,
        IEnumerable<(Guid DefinitionId, string CanonicalValue)> productValues,
        IEnumerable<(Guid DefinitionId, string CanonicalValue)> variantOverrides)
    {
        var productByDefinition = IndexByDefinition(productValues);
        var variantByDefinition = IndexByDefinition(variantOverrides);

        var effective = new Dictionary<Guid, string?>();
        foreach (var definitionId in unitByDefinition.Keys)
        {
            if (variantByDefinition.TryGetValue(definitionId, out var variantCanonical))
            {
                // PRESENT override (including invalid) owns this definition — never fall back.
                effective[definitionId] = variantCanonical;
                continue;
            }

            if (productByDefinition.TryGetValue(definitionId, out var productCanonical))
            {
                effective[definitionId] = productCanonical;
            }
        }

        long? agreed = null;
        var sawAny = false;
        foreach (var (definitionId, canonical) in effective)
        {
            if (!unitByDefinition.TryGetValue(definitionId, out var unit))
            {
                continue;
            }

            sawAny = true;
            var converted = TryConvertCanonicalToGrams(canonical, unit);
            if (converted is null)
            {
                return null;
            }

            if (agreed is null)
            {
                agreed = converted;
            }
            else if (agreed.Value != converted.Value)
            {
                return null;
            }
        }

        return sawAny ? agreed : null;
    }

    private static Dictionary<Guid, string> IndexByDefinition(
        IEnumerable<(Guid DefinitionId, string CanonicalValue)> values)
    {
        var map = new Dictionary<Guid, string>();
        foreach (var (definitionId, canonicalValue) in values)
        {
            // Last write wins for duplicate rows of the same definition (should not happen in DB).
            map[definitionId] = canonicalValue;
        }

        return map;
    }
}
