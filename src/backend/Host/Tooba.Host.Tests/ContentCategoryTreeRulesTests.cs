using Tooba.Content.Domain.Aggregates;
using Tooba.Content.Application.Categories.Commands;
using Tooba.Content.Application.Categories.Models;
using Tooba.Content.Application.Categories.Ports;
using Tooba.Content.Domain.Rules;
using Tooba.Content.Contracts.Errors;
using Tooba.BuildingBlocks;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>قواعد درخت دسته‌بندی مقاله — حداکثر عمق ۲.</summary>
public sealed class ContentCategoryTreeRulesTests
{
    [Fact]
    public void Move_rejects_self_parent()
    {
        var id = Guid.NewGuid();
        var maps = Maps((id, null, "fa-IR"));
        var ex = Assert.Throws<ContractOperationException>(() =>
            ContentCategoryTreeRules.ValidateMove(id, id, maps.ParentById, maps.LanguageById));
        Assert.Equal(ContentErrorCodes.CategorySelfParent, ex.Message);
    }

    [Fact]
    public void Move_rejects_cross_language_parent()
    {
        var fa = Guid.NewGuid();
        var en = Guid.NewGuid();
        var maps = Maps((fa, null, "fa-IR"), (en, null, "en-US"));
        var ex = Assert.Throws<ContractOperationException>(() =>
            ContentCategoryTreeRules.ValidateMove(fa, en, maps.ParentById, maps.LanguageById));
        Assert.Equal(ContentErrorCodes.CategoryCrossLanguageParent, ex.Message);
    }

    [Fact]
    public void Move_rejects_descendant_parent()
    {
        var root = Guid.NewGuid();
        var child = Guid.NewGuid();
        var maps = Maps((root, null, "fa-IR"), (child, root, "fa-IR"));
        var ex = Assert.Throws<ContractOperationException>(() =>
            ContentCategoryTreeRules.ValidateMove(root, child, maps.ParentById, maps.LanguageById));
        Assert.Equal(ContentErrorCodes.CategoryDescendantParent, ex.Message);
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

    [Fact]
    public void Create_rejects_level3_under_level2_parent()
    {
        var root = Guid.NewGuid();
        var child = Guid.NewGuid();
        var maps = Maps((root, null, "fa-IR"), (child, root, "fa-IR"));
        var ex = Assert.Throws<ContractOperationException>(() =>
            ContentCategoryTreeRules.ValidateCreateUnderParent(child, maps.ParentById));
        Assert.Equal(ContentErrorCodes.CategoryMaxDepthExceeded, ex.Message);
    }

    [Fact]
    public void Move_rejects_level1_with_child_under_level2()
    {
        var root = Guid.NewGuid();
        var child = Guid.NewGuid();
        var otherRoot = Guid.NewGuid();
        var otherChild = Guid.NewGuid();
        var maps = Maps(
            (root, null, "fa-IR"),
            (child, root, "fa-IR"),
            (otherRoot, null, "fa-IR"),
            (otherChild, otherRoot, "fa-IR"));
        var ex = Assert.Throws<ContractOperationException>(() =>
            ContentCategoryTreeRules.ValidateMove(root, otherChild, maps.ParentById, maps.LanguageById));
        Assert.Equal(ContentErrorCodes.CategoryMaxDepthExceeded, ex.Message);
    }

    [Fact]
    public void ComputeDepth_root_is_one_child_is_two()
    {
        var root = Guid.NewGuid();
        var child = Guid.NewGuid();
        var maps = Maps((root, null, "fa-IR"), (child, root, "fa-IR"));
        Assert.Equal(1, ContentCategoryTreeRules.ComputeDepth(root, maps.ParentById));
        Assert.Equal(2, ContentCategoryTreeRules.ComputeDepth(child, maps.ParentById));
        Assert.Equal(2, ContentCategoryTreeRules.MaxDepth);
    }

    private static (Dictionary<Guid, Guid?> ParentById, Dictionary<Guid, string> LanguageById) Maps(
        params (Guid Id, Guid? Parent, string Language)[] rows)
    {
        return (
            rows.ToDictionary(x => x.Id, x => x.Parent),
            rows.ToDictionary(x => x.Id, x => x.Language));
    }
}
