using Tooba.Catalog.Application.Brands;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>Non-DB parity tests for W20 Catalog brand-options projection.</summary>
public sealed class CatalogBrandOptionListBuilderTests
{
    [Fact]
    public void Empty_brands_returns_empty()
    {
        var result = BrandOptionListBuilder.Build([], new Dictionary<Guid, string>(), "x");
        Assert.Empty(result);
    }

    [Fact]
    public void Fa_IR_name_wins_over_other_locale_via_preferred_names()
    {
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var preferred = BrandOptionListBuilder.PreferredNames(
        [
            (id, "fa-IR", "فارسی"),
            (id, "en-US", "English"),
        ]);
        Assert.Equal("فارسی", preferred[id]);
        var rows = BrandOptionListBuilder.Build([(id, "slug", "Published")], preferred, null);
        Assert.Equal("فارسی", rows[0].Name);
    }

    [Fact]
    public void Fallback_to_other_locale_then_slug_then_brand_literal()
    {
        var a = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var b = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var c = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var preferred = BrandOptionListBuilder.PreferredNames([(a, "en-US", "Acme")]);
        var rows = BrandOptionListBuilder.Build(
        [
            (a, "ignored", "Draft"),
            (b, "slug-b", "Published"),
            (c, null, "Archived"),
        ],
        preferred,
        null);
        Assert.Equal(["Acme", "slug-b", "برند"], rows.Select(x => x.Name).ToArray());
    }

    [Fact]
    public void Search_trim_and_case_insensitive_contains_Ordinal_sort_and_limit_200()
    {
        var brands = Enumerable.Range(0, 250)
            .Select(i => (Guid.Parse($"{i:D8}-0000-0000-0000-000000000000"), (string?)null, "Draft"))
            .ToList();
        var preferred = brands.ToDictionary(
            b => b.Item1,
            b => "Brand-" + b.Item1.ToString("N")[..8]);
        preferred[brands[5].Item1] = "Zebra";
        preferred[brands[7].Item1] = "apple";
        preferred[brands[9].Item1] = "Apple-Pie";

        var filtered = BrandOptionListBuilder.Build(brands, preferred, "  ApPlE  ");
        Assert.All(filtered, x => Assert.Contains("apple", x.Name, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(filtered.OrderBy(x => x.Name, StringComparer.Ordinal).Select(x => x.Name), filtered.Select(x => x.Name));

        var limited = BrandOptionListBuilder.Build(brands, preferred, null);
        Assert.Equal(200, limited.Count);
        Assert.True(StringComparer.Ordinal.Compare(limited[0].Name, limited[^1].Name) <= 0);
    }

    [Fact]
    public void Status_string_parity()
    {
        var id = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var rows = BrandOptionListBuilder.Build([(id, "s", "Published")], new Dictionary<Guid, string>(), null);
        Assert.Equal("Published", rows[0].Status);
        Assert.Equal(id, rows[0].BrandId);
    }
}
