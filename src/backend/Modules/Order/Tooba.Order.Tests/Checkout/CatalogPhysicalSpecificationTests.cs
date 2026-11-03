using Tooba.Catalog.Domain.Aggregates;
using Tooba.Catalog.Domain.Enums;
using Tooba.Catalog.Domain.Rules;
using Xunit;

namespace Tooba.Order.Tests.Checkout;

/// <summary>TB-249-R5 Phase B: Catalog physical-specification authority (g/cm) + legacy weight policy.</summary>
public sealed class CatalogPhysicalSpecificationTests
{
    private static readonly Guid WeightDefId = Guid.Parse("01a05387-d58c-7000-8886-8ec4f64b05c8");

    [Fact]
    public void Typed_create_values_round_trip_exact_grams_and_centimetres()
    {
        var product = CatalogProduct.Create(CatalogProductKind.PhysicalGood, "phys-spec", DateTimeOffset.UtcNow);
        product.SetPhysicalSpecification(1500m, 20.5m, 15m, 10m, DateTimeOffset.UtcNow);

        Assert.Equal(1500m, product.WeightGrams);
        Assert.Equal(20.5m, product.LengthCentimeters);
        Assert.Equal(15m, product.WidthCentimeters);
        Assert.Equal(10m, product.HeightCentimeters);

        var facts = CatalogPhysicalSpecification.ResolveEffectiveFacts(
            product.WeightGrams,
            product.LengthCentimeters,
            product.WidthCentimeters,
            product.HeightCentimeters,
            variantWeightGrams: null,
            variantLengthCentimeters: null,
            variantWidthCentimeters: null,
            variantHeightCentimeters: null,
            legacyAttributeGrams: null);

        Assert.Equal(1500L, facts.WeightGrams);
        Assert.Equal(20.5m, facts.LengthCentimeters);
        Assert.Equal(15m, facts.WidthCentimeters);
        Assert.Equal(10m, facts.HeightCentimeters);
    }

    [Fact]
    public void Null_means_unspecified_and_never_fabricates_zero()
    {
        var product = CatalogProduct.Create(CatalogProductKind.PhysicalGood, "phys-null", DateTimeOffset.UtcNow);
        product.SetPhysicalSpecification(null, null, null, null, DateTimeOffset.UtcNow);

        Assert.Null(product.WeightGrams);
        Assert.Null(product.LengthCentimeters);
        Assert.Null(product.WidthCentimeters);
        Assert.Null(product.HeightCentimeters);

        var facts = CatalogPhysicalSpecification.ResolveEffectiveFacts(
            null, null, null, null, null, null, null, null, null);
        Assert.Null(facts.WeightGrams);
        Assert.Null(facts.LengthCentimeters);
        Assert.Null(facts.WidthCentimeters);
        Assert.Null(facts.HeightCentimeters);
    }

    [Fact]
    public void Negative_zero_overflow_and_excess_scale_are_rejected()
    {
        var product = CatalogProduct.Create(CatalogProductKind.PhysicalGood, "phys-bad", DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            product.SetPhysicalSpecification(0m, null, null, null, DateTimeOffset.UtcNow));
        Assert.Throws<InvalidOperationException>(() =>
            product.SetPhysicalSpecification(-1m, null, null, null, DateTimeOffset.UtcNow));
        Assert.Throws<InvalidOperationException>(() =>
            product.SetPhysicalSpecification(
                CatalogPhysicalSpecification.WeightGramsMaxValue + 1m,
                null,
                null,
                null,
                DateTimeOffset.UtcNow));
        Assert.Throws<InvalidOperationException>(() =>
            product.SetPhysicalSpecification(1.0001m, null, null, null, DateTimeOffset.UtcNow));
        Assert.Throws<InvalidOperationException>(() =>
            product.SetPhysicalSpecification(null, -2m, null, null, DateTimeOffset.UtcNow));
        Assert.Throws<InvalidOperationException>(() =>
            product.SetPhysicalSpecification(null, 1.001m, null, null, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Partial_state_allows_weight_without_dimensions()
    {
        var product = CatalogProduct.Create(CatalogProductKind.PhysicalGood, "phys-partial", DateTimeOffset.UtcNow);
        product.SetPhysicalSpecification(500m, null, null, null, DateTimeOffset.UtcNow);

        var facts = CatalogPhysicalSpecification.ResolveEffectiveFacts(
            product.WeightGrams,
            product.LengthCentimeters,
            product.WidthCentimeters,
            product.HeightCentimeters,
            null, null, null, null,
            null);

        Assert.Equal(500L, facts.WeightGrams);
        Assert.Null(facts.LengthCentimeters);
        Assert.Null(facts.WidthCentimeters);
        Assert.Null(facts.HeightCentimeters);
    }

    [Fact]
    public void Variant_override_replaces_product_and_null_inherits()
    {
        var product = CatalogProduct.Create(CatalogProductKind.PhysicalGood, "phys-var", DateTimeOffset.UtcNow);
        product.SetPhysicalSpecification(1000m, 30m, 20m, 10m, DateTimeOffset.UtcNow);

        var variant = CatalogVariant.Create(product.ProductId, "axis=a", null, DateTimeOffset.UtcNow);
        variant.SetPhysicalSpecification(1200m, 35m, null, null, DateTimeOffset.UtcNow);

        var overridden = CatalogPhysicalSpecification.ResolveEffectiveFacts(
            product.WeightGrams,
            product.LengthCentimeters,
            product.WidthCentimeters,
            product.HeightCentimeters,
            variant.WeightGrams,
            variant.LengthCentimeters,
            variant.WidthCentimeters,
            variant.HeightCentimeters,
            legacyAttributeGrams: null);

        Assert.Equal(1200L, overridden.WeightGrams);
        Assert.Equal(35m, overridden.LengthCentimeters);
        Assert.Equal(20m, overridden.WidthCentimeters); // inherit width
        Assert.Equal(10m, overridden.HeightCentimeters); // inherit height

        var inherited = CatalogPhysicalSpecification.ResolveEffectiveFacts(
            product.WeightGrams,
            product.LengthCentimeters,
            product.WidthCentimeters,
            product.HeightCentimeters,
            null, null, null, null,
            null);
        Assert.Equal(1000L, inherited.WeightGrams);
        Assert.Equal(30m, inherited.LengthCentimeters);
    }

    [Fact]
    public void Legacy_kg_attribute_compatible_when_typed_absent()
    {
        var units = new Dictionary<Guid, string> { [WeightDefId] = "kg" };
        var legacy = CatalogShippingWeightGrams.ResolveEffectiveGrams(
            units,
            [(WeightDefId, "1.4")],
            Array.Empty<(Guid, string)>());
        Assert.Equal(1400L, legacy);

        var facts = CatalogPhysicalSpecification.ResolveEffectiveFacts(
            null, null, null, null, null, null, null, null, legacy);
        Assert.Equal(1400L, facts.WeightGrams);
    }

    [Fact]
    public void Typed_and_legacy_agree_when_equal_and_fail_closed_when_conflict()
    {
        var agree = CatalogPhysicalSpecification.ResolveEffectiveWeightGrams(1400L, 1400L);
        Assert.Equal(1400L, agree);

        var conflict = CatalogPhysicalSpecification.ResolveEffectiveWeightGrams(1500L, 1400L);
        Assert.Null(conflict);

        var typedOnly = CatalogPhysicalSpecification.ResolveEffectiveWeightGrams(900L, null);
        Assert.Equal(900L, typedOnly);

        var legacyOnly = CatalogPhysicalSpecification.ResolveEffectiveWeightGrams(null, 800L);
        Assert.Equal(800L, legacyOnly);
    }

    [Fact]
    public void Fractional_typed_weight_is_unknown_for_whole_gram_shipping_read()
    {
        Assert.Null(CatalogPhysicalSpecification.TryToWholeGrams(1.5m));
        Assert.Equal(2L, CatalogPhysicalSpecification.TryToWholeGrams(2m));
    }

    [Fact]
    public void Present_invalid_legacy_override_stays_unknown_and_does_not_heal_from_typed()
    {
        // Attribute path: present invalid override fails closed (existing authority).
        var units = new Dictionary<Guid, string> { [WeightDefId] = "kg" };
        var legacy = CatalogShippingWeightGrams.ResolveEffectiveGrams(
            units,
            [(WeightDefId, "1.4")],
            [(WeightDefId, "0")]);
        Assert.Null(legacy);

        // Typed product 1400g + failed legacy → typed-only (legacy null after fail-closed).
        var facts = CatalogPhysicalSpecification.ResolveEffectiveFacts(
            1400m, null, null, null, null, null, null, null, legacy);
        Assert.Equal(1400L, facts.WeightGrams);

        // Typed 1500 + legacy 1400 (conflict) → unknown.
        var conflicted = CatalogPhysicalSpecification.ResolveEffectiveFacts(
            1500m, null, null, null, null, null, null, null, 1400L);
        Assert.Null(conflicted.WeightGrams);
    }

    [Fact]
    public void Clearing_typed_weight_restores_legacy_compatibility_path()
    {
        var product = CatalogProduct.Create(CatalogProductKind.PhysicalGood, "phys-clear", DateTimeOffset.UtcNow);
        product.SetPhysicalSpecification(2000m, 10m, 10m, 10m, DateTimeOffset.UtcNow);
        product.SetPhysicalSpecification(null, 10m, 10m, 10m, DateTimeOffset.UtcNow);

        Assert.Null(product.WeightGrams);
        var facts = CatalogPhysicalSpecification.ResolveEffectiveFacts(
            product.WeightGrams,
            product.LengthCentimeters,
            product.WidthCentimeters,
            product.HeightCentimeters,
            null, null, null, null,
            1800L);
        Assert.Equal(1800L, facts.WeightGrams);
        Assert.Equal(10m, facts.LengthCentimeters);
    }
}
