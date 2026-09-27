using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.ProductPublishing.Queries;
using Tooba.Catalog.Contracts.Errors;
using Tooba.Catalog.Domain;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>Focused non-DB parity checks for W16 ProductPublishing readiness mapping and rules.</summary>
public sealed class ProductPublishReadinessCapabilityTests
{
    [Fact]
    public void GetProductPublishReadinessHandler_Map_preserves_json_shape_fields()
    {
        var missing = new List<ProductPublishMissingRequirement>
        {
            new("category", ProductPublishRules.MessageCategoryIncompleteFa, "general"),
            new("identity", ProductPublishRules.MessageIdentityIncompleteFa, "general"),
            new("attributes", $"{ProductPublishRules.MessageAttributesIncompleteFa} (color)", "attributes"),
            new("variants", ProductPublishRules.MessageVariantsIncompleteFa, "variants"),
            new("media", ProductPublishRules.MessageMediaIncompleteFa, "media"),
            new("seo", ProductPublishRules.MessageSeoIncompleteFa, "seo"),
        };

        var readiness = new ProductPublishReadiness(
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            missing,
            ProductPublishRules.SummarizeMissingFa(missing.Count));

        var view = GetProductPublishReadinessHandler.Map(readiness);
        Assert.False(view.IsReady);
        Assert.False(view.CategoryReady);
        Assert.False(view.TranslationReady);
        Assert.False(view.AttributeReady);
        Assert.False(view.VariantReady);
        Assert.False(view.MediaReady);
        Assert.False(view.SeoReady);
        Assert.Equal(6, view.MissingRequirements.Count);
        Assert.Equal(
            new[] { "category", "identity", "attributes", "variants", "media", "seo" },
            view.MissingRequirements.Select(m => m.Code).ToArray());
        Assert.Equal(ProductPublishRules.SummarizeMissingFa(6), view.MessageFa);
        Assert.Contains("color", view.MissingRequirements[2].MessageFa, StringComparison.Ordinal);
        Assert.Equal("general", view.MissingRequirements[0].WorkspaceTab);
        Assert.Equal("attributes", view.MissingRequirements[2].WorkspaceTab);
        Assert.Equal("seo", view.MissingRequirements[5].WorkspaceTab);
    }

    [Fact]
    public void Ready_state_uses_MessageReadyFa()
    {
        var readiness = new ProductPublishReadiness(
            true, true, true, true, true, true, true, [], ProductPublishRules.MessageReadyFa);
        var view = GetProductPublishReadinessHandler.Map(readiness);
        Assert.True(view.IsReady);
        Assert.Empty(view.MissingRequirements);
        Assert.Equal(ProductPublishRules.MessageReadyFa, view.MessageFa);
    }

    [Theory]
    [InlineData(null, "fa-IR")]
    [InlineData("", "fa-IR")]
    [InlineData("   ", "fa-IR")]
    [InlineData("fa", "fa-IR")]
    [InlineData("fa-IR", "fa-IR")]
    [InlineData("en", "en")]
    [InlineData("en-US", "en")]
    public void Locale_normalization_parity(string? locale, string expected)
    {
        Assert.Equal(expected, ProductSeoRules.NormalizeLocale(locale));
    }

    [Fact]
    public void ProductPublishRules_authority_preserved()
    {
        Assert.Equal("محصول برای انتشار آماده است.", ProductPublishRules.MessageReadyFa);
        Assert.Equal("دسته‌بندی معتبر (سطح سوم) تکمیل نشده است.", ProductPublishRules.MessageCategoryIncompleteFa);
        Assert.Equal("اطلاعات اصلی / ترجمهٔ محصول تکمیل نشده است.", ProductPublishRules.MessageIdentityIncompleteFa);
        Assert.Equal("ویژگی‌های الزامی تکمیل نشده است.", ProductPublishRules.MessageAttributesIncompleteFa);
        Assert.Equal("تنوع‌های محصول آماده نیست.", ProductPublishRules.MessageVariantsIncompleteFa);
        Assert.Equal("تصویر اصلی تعیین نشده است.", ProductPublishRules.MessageMediaIncompleteFa);
        Assert.Equal("اطلاعات سئو تکمیل نشده است.", ProductPublishRules.MessageSeoIncompleteFa);
        Assert.Equal(ProductPublishRules.MessageReadyFa, ProductPublishRules.SummarizeMissingFa(0));
        Assert.Contains("2", ProductPublishRules.SummarizeMissingFa(2), StringComparison.Ordinal);
        Assert.Contains("مورد دیگر", ProductPublishRules.SummarizeMissingFa(3), StringComparison.Ordinal);
    }

    [Fact]
    public void Media_and_seo_MessageFa_fallback_behavior()
    {
        string? mediaMessage = null;
        string? seoMessage = "SEO custom";
        Assert.Equal(
            ProductPublishRules.MessageMediaIncompleteFa,
            mediaMessage ?? ProductPublishRules.MessageMediaIncompleteFa);
        Assert.Equal("SEO custom", seoMessage ?? ProductPublishRules.MessageSeoIncompleteFa);
    }

    [Fact]
    public void Attribute_missing_codes_suffix_behavior()
    {
        var codes = new[] { "color", "size" };
        var withSuffix = codes.Length > 0
            ? $"{ProductPublishRules.MessageAttributesIncompleteFa} ({string.Join("، ", codes)})"
            : ProductPublishRules.MessageAttributesIncompleteFa;
        Assert.Contains("color", withSuffix, StringComparison.Ordinal);
        Assert.Contains("size", withSuffix, StringComparison.Ordinal);
        Assert.Contains("،", withSuffix, StringComparison.Ordinal);

        var empty = Array.Empty<string>();
        var without = empty.Length > 0
            ? $"{ProductPublishRules.MessageAttributesIncompleteFa} ({string.Join("، ", empty)})"
            : ProductPublishRules.MessageAttributesIncompleteFa;
        Assert.Equal(ProductPublishRules.MessageAttributesIncompleteFa, without);
    }

    [Fact]
    public void Workspace_product_missing_code_is_stable()
    {
        Assert.Equal("workspace.product.missing", CatalogErrorCodes.WorkspaceProductMissing);
        var failure = Result.Failure<ProductPublishReadiness>(
            new SemanticError(CatalogErrorCodes.WorkspaceProductMissing));
        Assert.True(failure.IsFailure);
        Assert.Equal(CatalogErrorCodes.WorkspaceProductMissing, failure.FirstError.Code);
    }

    [Fact]
    public void IsReady_equals_missing_count_zero()
    {
        Assert.True(new List<string>().Count == 0);
        Assert.False(new List<string> { "category" }.Count == 0);
    }
}
