using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Host.Admin;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-P10-T013 — منو، عمق، مقصد امن، هدر و تصویر عمومی.</summary>
public sealed class StoreMenuComposerT013Tests
{
    [Fact]
    public async Task Cross_store_menu_reference_is_rejected()
    {
        var alpha = CreateComposer(out var db);
        var menu = await alpha.CreateAsync(new StoreMenuWriteRequest("منوی آلفا", "fa", "alpha-menu", true), CancellationToken.None);
        var beta = CreateComposer(out _);
        var missing = await Assert.ThrowsAsync<PlatformHttpException>(() => beta.GetAsync(menu.MenuId, CancellationToken.None));
        Assert.Equal("menu.missing", missing.ErrorCode);
        var ghost = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            beta.SetHeaderAsync(menu.MenuId, CancellationToken.None));
        Assert.Equal("menu.missing", ghost.ErrorCode);
        _ = db;
    }

    [Fact]
    public async Task Cycle_and_depth_violations_are_rejected()
    {
        var composer = CreateComposer(out _);
        var menu = await composer.CreateAsync(new StoreMenuWriteRequest("درخت", "fa", "tree-menu", true), CancellationToken.None);
        var l1 = await composer.AddItemAsync(menu.MenuId, new StoreMenuItemWriteRequest("یک", "Home", null, null, null, null, true), CancellationToken.None);
        var l2 = await composer.AddItemAsync(menu.MenuId, new StoreMenuItemWriteRequest("دو", "Group", l1.MenuItemId, null, null, null, true), CancellationToken.None);
        var l3 = await composer.AddItemAsync(menu.MenuId, new StoreMenuItemWriteRequest("سه", "Home", l2.MenuItemId, null, null, null, true), CancellationToken.None);
        var depth = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            composer.AddItemAsync(menu.MenuId, new StoreMenuItemWriteRequest("چهار", "Home", l3.MenuItemId, null, null, null, true), CancellationToken.None));
        Assert.Equal("menu.item.depth", depth.ErrorCode);

        var cycle = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            composer.UpdateItemAsync(
                menu.MenuId,
                l1.MenuItemId,
                new StoreMenuItemWriteRequest("یک", "Home", l3.MenuItemId, null, null, 0, true),
                CancellationToken.None));
        Assert.Equal("menu.item.cycle", cycle.ErrorCode);
    }

    [Fact]
    public async Task Unsafe_external_url_is_rejected()
    {
        var composer = CreateComposer(out _);
        var menu = await composer.CreateAsync(new StoreMenuWriteRequest("پیوند", "fa", "link-menu", true), CancellationToken.None);
        var js = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            composer.AddItemAsync(menu.MenuId, new StoreMenuItemWriteRequest("بد", "External", null, null, "javascript:alert(1)", null, true), CancellationToken.None));
        Assert.Equal("menu.url.unsafe", js.ErrorCode);
        var data = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            composer.AddItemAsync(menu.MenuId, new StoreMenuItemWriteRequest("داده", "External", null, null, "data:text/html,x", null, true), CancellationToken.None));
        Assert.Equal("menu.url.unsafe", data.ErrorCode);
        var ok = await composer.AddItemAsync(menu.MenuId, new StoreMenuItemWriteRequest("امن", "External", null, null, "https://example.com/shop", null, true), CancellationToken.None);
        Assert.Equal("https://example.com/shop", ok.ExternalUrl);
    }

    [Fact]
    public async Task Reorder_preserves_item_ids_and_sibling_order()
    {
        var composer = CreateComposer(out _);
        var menu = await composer.CreateAsync(new StoreMenuWriteRequest("ترتیب", "fa", "order-menu", true), CancellationToken.None);
        var a = await composer.AddItemAsync(menu.MenuId, new StoreMenuItemWriteRequest("الف", "Home", null, null, null, 0, true), CancellationToken.None);
        var b = await composer.AddItemAsync(menu.MenuId, new StoreMenuItemWriteRequest("ب", "Home", null, null, null, 1, true), CancellationToken.None);
        var reordered = await composer.ReorderItemsAsync(menu.MenuId, [b.MenuItemId, a.MenuItemId], CancellationToken.None);
        Assert.Equal(new[] { b.MenuItemId, a.MenuItemId }, reordered.Items.Select(x => x.MenuItemId).ToArray());
        Assert.Equal(0, reordered.Items.Single(x => x.MenuItemId == b.MenuItemId).SortOrder);
        Assert.Equal(1, reordered.Items.Single(x => x.MenuItemId == a.MenuItemId).SortOrder);
    }

    [Fact]
    public async Task Disabled_item_and_children_are_excluded_from_public_projection()
    {
        var composer = CreateComposer(out _);
        var menu = await composer.CreateAsync(new StoreMenuWriteRequest("عمومی", "fa", "public-menu", true), CancellationToken.None);
        var parent = await composer.AddItemAsync(menu.MenuId, new StoreMenuItemWriteRequest("والد", "Home", null, null, null, 0, true), CancellationToken.None);
        var child = await composer.AddItemAsync(menu.MenuId, new StoreMenuItemWriteRequest("فرزند", "Home", parent.MenuItemId, null, null, 0, true), CancellationToken.None);
        var other = await composer.AddItemAsync(menu.MenuId, new StoreMenuItemWriteRequest("دیگر", "Home", null, null, null, 1, true), CancellationToken.None);
        await composer.SetItemEnabledAsync(menu.MenuId, parent.MenuItemId, false, CancellationToken.None);
        var projected = await composer.ProjectPublicAsync(menu.MenuId, CancellationToken.None);
        Assert.Equal(new[] { other.MenuItemId }, projected.Select(x => x.MenuItemId).ToArray());
        Assert.DoesNotContain(projected, x => x.MenuItemId == child.MenuItemId);
    }

    [Fact]
    public async Task Selected_header_menu_resolves_and_unset_uses_fallback()
    {
        var composer = CreateComposer(out _);
        var menu = await composer.CreateAsync(new StoreMenuWriteRequest("هدر", "fa", "header-menu", true), CancellationToken.None);
        var l1 = await composer.AddItemAsync(menu.MenuId, new StoreMenuItemWriteRequest("خانه", "Home", null, null, null, 0, true), CancellationToken.None);
        var l2 = await composer.AddItemAsync(menu.MenuId, new StoreMenuItemWriteRequest("گروه", "Group", l1.MenuItemId, null, null, 0, true), CancellationToken.None);
        var l3 = await composer.AddItemAsync(menu.MenuId, new StoreMenuItemWriteRequest("سوم", "Home", l2.MenuItemId, null, null, 0, true), CancellationToken.None);

        var unset = await composer.GetHeaderPublicAsync(CancellationToken.None);
        Assert.True(unset.UsesFallback);
        Assert.Empty(unset.Items);

        var selected = await composer.SetHeaderAsync(menu.MenuId, CancellationToken.None);
        Assert.False(selected.UsesFallback);
        var publicHeader = await composer.GetHeaderPublicAsync(CancellationToken.None);
        Assert.False(publicHeader.UsesFallback);
        Assert.Equal(3, publicHeader.Items.Count);
        Assert.Contains(publicHeader.Items, x => x.MenuItemId == l3.MenuItemId && x.Depth == 3);

        var cleared = await composer.SetHeaderAsync(null, CancellationToken.None);
        Assert.True(cleared.UsesFallback);
        Assert.True((await composer.GetHeaderPublicAsync(CancellationToken.None)).UsesFallback);
    }

    [Fact]
    public async Task Disabled_menu_cannot_be_header_and_unknown_landing_target_is_rejected()
    {
        var composer = CreateComposer(out _);
        var menu = await composer.CreateAsync(new StoreMenuWriteRequest("خاموش", "fa", "off-menu", true), CancellationToken.None);
        await composer.SetEnabledAsync(menu.MenuId, false, CancellationToken.None);
        var ineligible = await Assert.ThrowsAsync<PlatformHttpException>(() => composer.SetHeaderAsync(menu.MenuId, CancellationToken.None));
        Assert.Equal("menu.header.ineligible", ineligible.ErrorCode);

        var live = await composer.CreateAsync(new StoreMenuWriteRequest("زنده", "fa", "live-menu", true), CancellationToken.None);
        var missingTarget = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            composer.AddItemAsync(live.MenuId, new StoreMenuItemWriteRequest("فرود", "LandingPage", null, Guid.NewGuid(), null, null, true), CancellationToken.None));
        Assert.Equal("menu.target.missing", missingTarget.ErrorCode);
    }

    private static StoreMenuComposer CreateComposer(out CatalogDbContext catalog) =>
        StoreMenuComposerTestFactory.Create(out catalog);
}
