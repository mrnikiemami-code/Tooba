using Tooba.Catalog.Domain;
using Xunit;

namespace Tooba.Host.Tests;

public sealed class VariantAxisCapabilityRulesTests
{
    [Theory]
    [InlineData(CatalogAttributeValueKind.Boolean)]
    [InlineData(CatalogAttributeValueKind.Text)]
    [InlineData(CatalogAttributeValueKind.Instant)]
    public void ValueKind_blocks_capability_enable(CatalogAttributeValueKind kind)
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            CatalogCategoryAttributeAssignmentRules.ValidateVariantAxisCapabilityEnable(kind));
        Assert.Equal("catalog.attribute.variant_axis.value_kind.invalid", ex.Message);
    }

    [Theory]
    [InlineData(CatalogAttributeValueKind.Enumeration)]
    [InlineData(CatalogAttributeValueKind.Number)]
    public void ValueKind_allows_capability_enable(CatalogAttributeValueKind kind)
    {
        var ex = Record.Exception(() =>
            CatalogCategoryAttributeAssignmentRules.ValidateVariantAxisCapabilityEnable(kind));
        Assert.Null(ex);
    }

    [Fact]
    public void Definition_set_variant_allowed_for_enumeration()
    {
        var def = CatalogAttributeDefinition.Create("color", CatalogAttributeValueKind.Enumeration, false, DateTimeOffset.UtcNow);
        def.SetVariantAxisAllowed(true);
        Assert.True(def.IsVariantAxisAllowed);
    }

    [Fact]
    public void ValidateVariantAxis_rejects_capability_disabled()
    {
        var def = CatalogAttributeDefinition.Create("screen", CatalogAttributeValueKind.Number, false, DateTimeOffset.UtcNow);
        var ex = Assert.Throws<InvalidOperationException>(() =>
            CatalogCategoryAttributeAssignmentRules.ValidateVariantAxis(def, true));
        Assert.Equal("catalog.attribute.variant_axis.capability_disabled", ex.Message);
    }
}
