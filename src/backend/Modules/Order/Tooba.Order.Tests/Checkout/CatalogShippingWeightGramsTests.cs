using Tooba.Catalog.Domain.Enums;
using Tooba.Catalog.Domain.Rules;
using Xunit;

namespace Tooba.Order.Tests.Checkout;

/// <summary>TB-243-R1: focused facts for Catalog shipping-weight semantic authority.</summary>
public sealed class CatalogShippingWeightGramsTests
{
    private static readonly Guid LaptopWeightId = Guid.Parse("01a05387-d58c-7000-8886-8ec4f64b05c8");
    private static readonly Guid MobileWeightId = Guid.Parse("01a05387-d504-7000-b590-c01ae663fd89");
    private static readonly Guid FoodVolumeId = Guid.Parse("01a05387-d76f-7000-ab00-9728ab657b4a");

    [Fact]
    public void Established_weight_codes_are_recognized_and_unrelated_g_unit_is_excluded()
    {
        Assert.True(CatalogShippingWeightGrams.IsShippingWeightDefinition(
            "demo_attr_laptop_weight", CatalogAttributeValueKind.Number, isActive: true, unit: "kg"));
        Assert.True(CatalogShippingWeightGrams.IsShippingWeightDefinition(
            "weight", CatalogAttributeValueKind.Number, isActive: true, unit: "g"));
        Assert.False(CatalogShippingWeightGrams.IsShippingWeightDefinition(
            "demo_attr_food_volume", CatalogAttributeValueKind.Number, isActive: true, unit: "g"));
        Assert.False(CatalogShippingWeightGrams.IsShippingWeightDefinition(
            "demo_attr_laptop_weight", CatalogAttributeValueKind.Number, isActive: false, unit: "kg"));
        Assert.False(CatalogShippingWeightGrams.IsShippingWeightDefinition(
            "demo_attr_laptop_weight", CatalogAttributeValueKind.Text, isActive: true, unit: "kg"));
    }

    [Fact]
    public void Product_1_4_kg_converts_to_exact_1400_grams()
    {
        var units = new Dictionary<Guid, string> { [LaptopWeightId] = "kg" };
        var grams = CatalogShippingWeightGrams.ResolveEffectiveGrams(
            units,
            [(LaptopWeightId, "1.4")],
            Array.Empty<(Guid, string)>());
        Assert.Equal(1400L, grams);
    }

    [Fact]
    public void Same_definition_variant_override_replaces_product_value()
    {
        var units = new Dictionary<Guid, string> { [LaptopWeightId] = "kg" };
        var grams = CatalogShippingWeightGrams.ResolveEffectiveGrams(
            units,
            [(LaptopWeightId, "1.4")],
            [(LaptopWeightId, "2.0")]);
        Assert.Equal(2000L, grams);
    }

    [Fact]
    public void Absent_variant_override_inherits_product_value()
    {
        var units = new Dictionary<Guid, string> { [LaptopWeightId] = "kg" };
        var grams = CatalogShippingWeightGrams.ResolveEffectiveGrams(
            units,
            [(LaptopWeightId, "1.4")],
            Array.Empty<(Guid, string)>());
        Assert.Equal(1400L, grams);
    }

    [Fact]
    public void Present_invalid_variant_override_does_not_heal_from_product()
    {
        var units = new Dictionary<Guid, string> { [LaptopWeightId] = "kg" };
        var grams = CatalogShippingWeightGrams.ResolveEffectiveGrams(
            units,
            [(LaptopWeightId, "1.4")],
            [(LaptopWeightId, "0")]);
        Assert.Null(grams);

        grams = CatalogShippingWeightGrams.ResolveEffectiveGrams(
            units,
            [(LaptopWeightId, "1.4")],
            [(LaptopWeightId, "-1")]);
        Assert.Null(grams);

        grams = CatalogShippingWeightGrams.ResolveEffectiveGrams(
            units,
            [(LaptopWeightId, "1.4")],
            [(LaptopWeightId, "not-a-number")]);
        Assert.Null(grams);
    }

    [Fact]
    public void Conflicting_effective_definitions_are_unknown()
    {
        var units = new Dictionary<Guid, string>
        {
            [LaptopWeightId] = "kg",
            [MobileWeightId] = "g",
        };
        var grams = CatalogShippingWeightGrams.ResolveEffectiveGrams(
            units,
            [(LaptopWeightId, "1.4"), (MobileWeightId, "1500")],
            Array.Empty<(Guid, string)>());
        Assert.Null(grams);
    }

    [Fact]
    public void Unrelated_or_inactive_kg_definitions_are_not_authority()
    {
        Assert.False(CatalogShippingWeightGrams.IsShippingWeightDefinition(
            "demo_attr_food_volume", CatalogAttributeValueKind.Number, isActive: true, unit: "g"));
        Assert.Null(CatalogShippingWeightGrams.TryConvertCanonicalToGrams("1.4", "lb"));
    }

    [Fact]
    public void Malformed_comma_string_and_fractional_grams_and_overflow_are_unknown()
    {
        Assert.Null(CatalogShippingWeightGrams.TryConvertCanonicalToGrams("1,4", "kg"));
        Assert.Null(CatalogShippingWeightGrams.TryConvertCanonicalToGrams("1.0005", "g"));
        Assert.Null(CatalogShippingWeightGrams.TryConvertCanonicalToGrams(
            decimal.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "kg"));
    }

    [Fact]
    public void Agreeing_definitions_in_kg_and_g_resolve_once()
    {
        var units = new Dictionary<Guid, string>
        {
            [LaptopWeightId] = "kg",
            [MobileWeightId] = "g",
        };
        var grams = CatalogShippingWeightGrams.ResolveEffectiveGrams(
            units,
            [(LaptopWeightId, "1.4"), (MobileWeightId, "1400")],
            Array.Empty<(Guid, string)>());
        Assert.Equal(1400L, grams);
    }
}
