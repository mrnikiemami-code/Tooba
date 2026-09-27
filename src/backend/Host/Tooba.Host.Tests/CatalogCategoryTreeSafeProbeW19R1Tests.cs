using Tooba.Catalog.Domain;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-TMAR-HOST-ADMIN-AMC-001-W19-R1 — Domain Try* category-level / assignability probes.
/// </summary>
public sealed class CatalogCategoryTreeSafeProbeW19R1Tests
{
    private static readonly Guid L1 = Guid.Parse("aaaaaaaa-aaaa-7aaa-8aaa-aaaaaaaaaaa1");
    private static readonly Guid L2 = Guid.Parse("aaaaaaaa-aaaa-7aaa-8aaa-aaaaaaaaaaa2");
    private static readonly Guid L3 = Guid.Parse("aaaaaaaa-aaaa-7aaa-8aaa-aaaaaaaaaaa3");

    private static Dictionary<Guid, Guid?> ValidTree() => new()
    {
        [L1] = null,
        [L2] = L1,
        [L3] = L2,
    };

    [Fact]
    public void Valid_L1_L2_not_assignable_L3_assignable_via_Try_and_throwing_APIs()
    {
        var parentById = ValidTree();

        Assert.True(CatalogCategoryTreeRules.TryGetCategoryLevel(L1, parentById, out var level1));
        Assert.Equal(1, level1);
        Assert.Equal(1, CatalogCategoryTreeRules.GetCategoryLevel(L1, parentById));
        Assert.True(CatalogCategoryTreeRules.TryIsAssignableProductCategory(L1, parentById, out var a1));
        Assert.False(a1);
        Assert.False(CatalogCategoryTreeRules.IsAssignableProductCategory(L1, parentById));

        Assert.True(CatalogCategoryTreeRules.TryGetCategoryLevel(L2, parentById, out var level2));
        Assert.Equal(2, level2);
        Assert.Equal(2, CatalogCategoryTreeRules.GetCategoryLevel(L2, parentById));
        Assert.True(CatalogCategoryTreeRules.TryIsAssignableProductCategory(L2, parentById, out var a2));
        Assert.False(a2);
        Assert.False(CatalogCategoryTreeRules.IsAssignableProductCategory(L2, parentById));

        Assert.True(CatalogCategoryTreeRules.TryGetCategoryLevel(L3, parentById, out var level3));
        Assert.Equal(3, level3);
        Assert.Equal(3, CatalogCategoryTreeRules.GetCategoryLevel(L3, parentById));
        Assert.True(CatalogCategoryTreeRules.TryIsAssignableProductCategory(L3, parentById, out var a3));
        Assert.True(a3);
        Assert.True(CatalogCategoryTreeRules.IsAssignableProductCategory(L3, parentById));
    }

    [Fact]
    public void Missing_category_safe_probe_returns_false_throwing_API_still_throws()
    {
        var parentById = ValidTree();
        var missing = Guid.Parse("bbbbbbbb-bbbb-7bbb-8bbb-bbbbbbbbbbbb");

        Assert.False(CatalogCategoryTreeRules.TryGetCategoryLevel(missing, parentById, out var level));
        Assert.Equal(0, level);
        Assert.False(CatalogCategoryTreeRules.TryIsAssignableProductCategory(missing, parentById, out var assignable));
        Assert.False(assignable);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            CatalogCategoryTreeRules.GetCategoryLevel(missing, parentById));
        Assert.Equal("رده در Catalog این Tenant وجود ندارد.", ex.Message);
        Assert.Throws<InvalidOperationException>(() =>
            CatalogCategoryTreeRules.IsAssignableProductCategory(missing, parentById));
    }

    [Fact]
    public void Cycle_safe_probe_returns_false_throwing_API_still_throws()
    {
        var a = Guid.Parse("cccccccc-cccc-7ccc-8ccc-ccccccccccc1");
        var b = Guid.Parse("cccccccc-cccc-7ccc-8ccc-ccccccccccc2");
        var parentById = new Dictionary<Guid, Guid?>
        {
            [a] = b,
            [b] = a,
        };

        Assert.False(CatalogCategoryTreeRules.TryGetCategoryLevel(a, parentById, out var level));
        Assert.Equal(0, level);
        Assert.False(CatalogCategoryTreeRules.TryIsAssignableProductCategory(a, parentById, out var assignable));
        Assert.False(assignable);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            CatalogCategoryTreeRules.GetCategoryLevel(a, parentById));
        Assert.Equal("حلقهٔ موجود در درخت رده تشخیص داده شد.", ex.Message);
        Assert.Throws<InvalidOperationException>(() =>
            CatalogCategoryTreeRules.IsAssignableProductCategory(a, parentById));
    }

    [Fact]
    public void EnsureAssignable_compatibility_preserved_for_valid_non_L3()
    {
        var parentById = ValidTree();
        var ex = Assert.Throws<InvalidOperationException>(() =>
            CatalogCategoryTreeRules.EnsureAssignableProductCategory(L1, parentById));
        Assert.Equal(CatalogCategoryTreeRules.ProductAssignableLevelRequiredMessageFa, ex.Message);
    }
}
