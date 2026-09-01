using Tooba.Host.Localization;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>قواعد رجیستری زبان کانونی.</summary>
public sealed class SupportedLocaleRegistryTests
{
    [Fact]
    public void List_returns_fa_IR_default_and_en_US()
    {
        var registry = new SupportedLocaleRegistry();
        var locales = registry.List();
        Assert.Equal(2, locales.Count);
        Assert.Contains(locales, x => x.Code == "fa-IR" && x is { Active: true, IsDefault: true });
        Assert.Contains(locales, x => x.Code == "en-US" && x is { Active: true, IsDefault: false });
    }

    [Fact]
    public void Patch_cannot_deactivate_default_without_replacement()
    {
        var registry = new SupportedLocaleRegistry();
        Assert.Throws<InvalidOperationException>(() =>
            registry.Patch("fa-IR", new SupportedLocalePatch(Active: false, IsDefault: true, SortOrder: null)));
    }

    [Fact]
    public void Patch_can_move_default_to_en_US()
    {
        var registry = new SupportedLocaleRegistry();
        var updated = registry.Patch("en-US", new SupportedLocalePatch(null, IsDefault: true, null));
        Assert.True(updated.IsDefault);
        var locales = registry.List();
        Assert.Single(locales.Where(x => x is { Active: true, IsDefault: true }));
        Assert.Equal("en-US", locales.Single(x => x.IsDefault).Code);
    }
}
