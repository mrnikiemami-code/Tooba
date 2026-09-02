using Tooba.Content.Domain;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>قواعد درخت دسته‌بندی مقاله — بدون وابستگی DB.</summary>
public sealed class ContentCategoryTreeRulesTests
{
    [Fact]
    public void Move_rejects_self_parent()
    {
        var id = Guid.NewGuid();
        var maps = Maps((id, null, "fa-IR"));
        var ex = Assert.Throws<InvalidOperationException>(() =>
            ContentCategoryTreeRules.ValidateMove(id, id, maps.ParentById, maps.LanguageById));
        Assert.Equal(ContentCategoryErrorCodes.SelfParent, ex.Message);
    }

    [Fact]
    public void Move_rejects_cross_language_parent()
    {
        var fa = Guid.NewGuid();
        var en = Guid.NewGuid();
        var maps = Maps((fa, null, "fa-IR"), (en, null, "en-US"));
        var ex = Assert.Throws<InvalidOperationException>(() =>
            ContentCategoryTreeRules.ValidateMove(fa, en, maps.ParentById, maps.LanguageById));
        Assert.Equal(ContentCategoryErrorCodes.CrossLanguageParent, ex.Message);
    }

    [Fact]
    public void Move_rejects_descendant_parent()
    {
        var root = Guid.NewGuid();
        var child = Guid.NewGuid();
        var maps = Maps((root, null, "fa-IR"), (child, root, "fa-IR"));
        var ex = Assert.Throws<InvalidOperationException>(() =>
            ContentCategoryTreeRules.ValidateMove(root, child, maps.ParentById, maps.LanguageById));
        Assert.Equal(ContentCategoryErrorCodes.DescendantParent, ex.Message);
    }

    [Fact]
    public void IsDescendant_detects_nested_nodes()
    {
        var root = Guid.NewGuid();
        var child = Guid.NewGuid();
        var grand = Guid.NewGuid();
        var maps = Maps((root, null, "fa-IR"), (child, root, "fa-IR"), (grand, child, "fa-IR"));
        Assert.True(ContentCategoryTreeRules.IsDescendant(root, grand, maps.ParentById));
        Assert.False(ContentCategoryTreeRules.IsDescendant(child, root, maps.ParentById));
    }

    private static (Dictionary<Guid, Guid?> ParentById, Dictionary<Guid, string> LanguageById) Maps(
        params (Guid Id, Guid? Parent, string Language)[] rows)
    {
        return (
            rows.ToDictionary(x => x.Id, x => x.Parent),
            rows.ToDictionary(x => x.Id, x => x.Language));
    }
}
