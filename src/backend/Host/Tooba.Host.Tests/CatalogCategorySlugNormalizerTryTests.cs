using Tooba.Catalog.Domain;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-TMAR-HOST-ADMIN-AMC-001-W14-R1 — Domain Try* slug path parity with throwing APIs.
/// </summary>
public sealed class CatalogCategorySlugNormalizerTryTests
{
    [Theory]
    [InlineData("  Summer Shirts  ", "summer-shirts")]
    [InlineData("linen_shirt", "linen-shirt")]
    [InlineData("a/b\\c", "a-b-c")]
    [InlineData("foo---bar", "foo-bar")]
    [InlineData("Galaxy-S24", "galaxy-s24")]
    public void TryNormalizeSlug_valid_ascii_matches_NormalizeSlug(string input, string expected)
    {
        Assert.True(CatalogCategorySlugNormalizer.TryNormalizeSlug(input, out var tryResult));
        Assert.Equal(expected, tryResult);
        Assert.Equal(CatalogCategorySlugNormalizer.NormalizeSlug(input), tryResult);
    }

    [Fact]
    public void TryNormalizeSlug_persian_parity_with_NormalizeSlug()
    {
        const string input = "گوشی سامسونگ Galaxy S24";
        Assert.True(CatalogCategorySlugNormalizer.TryNormalizeSlug(input, out var tryResult));
        Assert.Equal("گوشی-سامسونگ-galaxy-s24", tryResult);
        Assert.Equal(CatalogCategorySlugNormalizer.NormalizeSlug(input), tryResult);
    }

    [Fact]
    public void TrySlugifyFromName_parity_with_SlugifyFromName()
    {
        const string name = "گوشی موبایل";
        Assert.True(CatalogCategorySlugNormalizer.TrySlugifyFromName(name, out var tryResult));
        Assert.Equal(CatalogCategorySlugNormalizer.SlugifyFromName(name), tryResult);
        Assert.Equal("گوشی-موبایل", tryResult);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("   ---   ")]
    [InlineData("___")]
    [InlineData("///\\\\\\")]
    [InlineData("...")]
    public void TryNormalizeSlug_invalid_returns_false(string? input)
    {
        Assert.False(CatalogCategorySlugNormalizer.TryNormalizeSlug(input, out var normalized));
        Assert.Equal(string.Empty, normalized);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ---   ")]
    public void TrySlugifyFromName_invalid_returns_false(string? name)
    {
        Assert.False(CatalogCategorySlugNormalizer.TrySlugifyFromName(name, out var normalized));
        Assert.Equal(string.Empty, normalized);
    }

    [Fact]
    public void Existing_throwing_APIs_remain_compatible()
    {
        Assert.Equal("summer-shirts", CatalogCategorySlugNormalizer.NormalizeSlug("  Summer Shirts  "));
        Assert.Equal("گوشی-موبایل", CatalogCategorySlugNormalizer.SlugifyFromName("گوشی موبایل"));
        Assert.Throws<ArgumentException>(() => CatalogCategorySlugNormalizer.NormalizeSlug("   "));
        Assert.Throws<InvalidOperationException>(() => CatalogCategorySlugNormalizer.NormalizeSlug("   ---   "));
    }
}
