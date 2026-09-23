using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Host.Admin;
using Tooba.Order.Application;
using Tooba.Order.Application.Checkout.Abuse;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Checkout.Policies;
using Tooba.Order.Application.Checkout.Process;
using Tooba.Order.Application.PurchaseVerification;
using Tooba.Order.Application.ReservationCycle.Contracts;
using Tooba.Order.Application.ReservationCycle.Policies;
using Tooba.Order.Application.ReservationCycle.Services;
using Tooba.Order.Application.Seller.Policies;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure;
using Tooba.Order.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-P10-T004-R18 — Admin UX سیاست رزرو Store/Category/Offer.</summary>
public sealed class ReservationPolicyAdminUxTests
{
    [Fact]
    public async Task Preview_platform_store_category_offer_and_clear_restore_inheritance()
    {
        await using var catalog = CreateCatalog();
        var store = StoreHoldPolicySettings.CreateDefault(DateTimeOffset.UtcNow);
        store.ReplaceReservationCycle(30, 20, 4, DateTimeOffset.UtcNow);
        catalog.StoreHoldPolicySettings.Add(store);
        var categoryId = Guid.NewGuid();
        var offerId = Guid.NewGuid();
        catalog.ReservationCyclePolicyOverrides.Add(ReservationCyclePolicyOverride.Create(
            ReservationCyclePolicyOverride.CategoryScope, categoryId, 15, null, 2, DateTimeOffset.UtcNow));
        catalog.ReservationCyclePolicyOverrides.Add(ReservationCyclePolicyOverride.Create(
            ReservationCyclePolicyOverride.OfferScope, offerId, 3, 2, null, DateTimeOffset.UtcNow));
        await catalog.SaveChangesAsync();
        var resolver = Resolver(catalog, 120, 90, 3);

        var storePreview = ReservationPolicyAdminComposer.ForStore(
            await resolver.PreviewAsync(null, null, CancellationToken.None), true);
        Assert.Equal(30, storePreview.InitialHold.EffectiveValue);
        Assert.Equal("store", storePreview.InitialHold.Source);
        Assert.True(storePreview.InitialHold.Overridden);

        var categoryPreview = ReservationPolicyAdminComposer.ForCategory(
            categoryId, await resolver.PreviewAsync(null, categoryId, CancellationToken.None), true);
        Assert.Equal(15, categoryPreview.InitialHold.EffectiveValue);
        Assert.Equal("category", categoryPreview.InitialHold.Source);
        Assert.Equal(20, categoryPreview.RetryHold.EffectiveValue);
        Assert.Equal("store", categoryPreview.RetryHold.Source);
        Assert.False(categoryPreview.RetryHold.Overridden);
        Assert.Equal(2, categoryPreview.MaxCycles.EffectiveValue);

        var offerPreview = ReservationPolicyAdminComposer.ForOffer(
            offerId, await resolver.PreviewAsync(offerId, categoryId, CancellationToken.None), true);
        Assert.Equal(3, offerPreview.InitialHold.EffectiveValue);
        Assert.Equal("offer", offerPreview.InitialHold.Source);
        Assert.Equal(2, offerPreview.RetryHold.EffectiveValue);
        Assert.Equal("offer", offerPreview.RetryHold.Source);
        Assert.Equal(2, offerPreview.MaxCycles.EffectiveValue);
        Assert.Equal("category", offerPreview.MaxCycles.Source);
        Assert.True(offerPreview.FlashSaleStricter);

        await ReservationPolicyAdminComposer.ReplaceOverrideAsync(
            catalog,
            ReservationCyclePolicyOverride.OfferScope,
            offerId,
            new ReservationPolicyWriteRequest(null, null, null),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            CancellationToken.None);
        var afterOfferClear = ReservationPolicyAdminComposer.ForOffer(
            offerId, await resolver.PreviewAsync(offerId, categoryId, CancellationToken.None), true);
        Assert.Equal(15, afterOfferClear.InitialHold.EffectiveValue);
        Assert.Equal("category", afterOfferClear.InitialHold.Source);
        Assert.False(afterOfferClear.InitialHold.Overridden);

        await ReservationPolicyAdminComposer.ReplaceOverrideAsync(
            catalog,
            ReservationCyclePolicyOverride.CategoryScope,
            categoryId,
            new ReservationPolicyWriteRequest(null, null, null),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            CancellationToken.None);
        var afterCategoryClear = ReservationPolicyAdminComposer.ForCategory(
            categoryId, await resolver.PreviewAsync(null, categoryId, CancellationToken.None), true);
        Assert.Equal(30, afterCategoryClear.InitialHold.EffectiveValue);
        Assert.Equal("store", afterCategoryClear.InitialHold.Source);
        Assert.False(afterCategoryClear.InitialHold.Overridden);
    }

    [Fact]
    public void Validation_rejects_non_positive_and_out_of_range_without_coercion()
    {
        Assert.Throws<PlatformHttpException>(() =>
            ReservationPolicyAdminComposer.ValidateWrite(new ReservationPolicyWriteRequest(0, 10, 2)));
        Assert.Throws<PlatformHttpException>(() =>
            ReservationPolicyAdminComposer.ValidateWrite(new ReservationPolicyWriteRequest(10, 0, 2)));
        Assert.Throws<PlatformHttpException>(() =>
            ReservationPolicyAdminComposer.ValidateWrite(new ReservationPolicyWriteRequest(10, 10, 0)));
        Assert.Throws<PlatformHttpException>(() =>
            ReservationPolicyAdminComposer.ValidateWrite(new ReservationPolicyWriteRequest(-1, 10, 2)));
        Assert.Throws<PlatformHttpException>(() =>
            ReservationPolicyAdminComposer.ValidateWrite(new ReservationPolicyWriteRequest(10, 10, 21)));
        Assert.Throws<PlatformHttpException>(() =>
            ReservationPolicyAdminComposer.ValidateWrite(new ReservationPolicyWriteRequest(43201, 10, 2)));
        ReservationPolicyAdminComposer.ValidateWrite(new ReservationPolicyWriteRequest(1, 1, 1));
        ReservationPolicyAdminComposer.ValidateWrite(new ReservationPolicyWriteRequest(43200, 43200, 20));
        ReservationPolicyAdminComposer.ValidateWrite(new ReservationPolicyWriteRequest(null, null, null));
    }

    [Fact]
    public async Task Valid_boundaries_persist_and_audit_records_field_changes()
    {
        await using var catalog = CreateCatalog();
        var actor = Guid.NewGuid();
        var now = DateTimeOffset.Parse("2026-09-13T10:00:00Z");
        var store = StoreHoldPolicySettings.CreateDefault(now);
        catalog.StoreHoldPolicySettings.Add(store);
        ReservationPolicyAdminComposer.ReplaceStore(
            store,
            new ReservationPolicyWriteRequest(1, 43200, 20),
            actor,
            now,
            catalog);
        await catalog.SaveChangesAsync();
        Assert.Equal(1, store.InitialReservationHoldMinutes);
        Assert.Equal(43200, store.RetryReservationHoldMinutes);
        Assert.Equal(20, store.MaxReservationCycles);
        Assert.Equal(3, catalog.ReservationPolicyAuditEvents.Count());
        Assert.All(catalog.ReservationPolicyAuditEvents, x =>
        {
            Assert.Equal("store", x.Level);
            Assert.Equal(actor, x.ActorUserId);
            Assert.Equal(now, x.OccurredAt);
            Assert.Null(x.OldOverride);
        });
    }

    [Fact]
    public async Task Settings_do_not_mutate_active_or_historical_cycle_snapshots()
    {
        await using var orders = CreateOrders();
        await using var catalog = CreateCatalog();
        var dir = new ReservationCycleDirectory(orders);
        var checkout = Guid.NewGuid();
        var t0 = DateTimeOffset.Parse("2026-09-13T10:00:00Z");
        var firstPolicy = new ReservationCyclePolicySnapshot(3, 2, 2, "offer");
        var first = await dir.StartAsync(
            checkout, ReservationCycleReason.InitialPayment, t0, t0.AddMinutes(3), firstPolicy,
            [Guid.NewGuid()], "commit", null, null, CancellationToken.None);
        var expires = first.ExpiresAt;
        var store = StoreHoldPolicySettings.CreateDefault(t0);
        catalog.StoreHoldPolicySettings.Add(store);
        ReservationPolicyAdminComposer.ReplaceStore(
            store, new ReservationPolicyWriteRequest(10, 8, 5), Guid.NewGuid(), t0.AddMinutes(1), catalog);
        await catalog.SaveChangesAsync();
        var active = await dir.GetActiveAsync(checkout, CancellationToken.None);
        Assert.Equal(expires, active!.ExpiresAt);
        Assert.Equal(3, active.EffectiveHoldMinutes);
        Assert.Equal(2, active.EffectiveMaxCycles);
        Assert.Equal("offer", active.PolicySource);

        await dir.CloseActiveAsync(checkout, ReservationCycleStatus.Expired, t0.AddMinutes(3), CancellationToken.None);
        var resolver = Resolver(catalog, 120, 120, 3);
        var nextPolicy = await resolver.ResolveAsync([], CancellationToken.None);
        Assert.Equal(10, nextPolicy.InitialHoldMinutes);
        var second = await dir.StartAsync(
            checkout, ReservationCycleReason.RetryAfterExpiry, t0.AddMinutes(4), t0.AddMinutes(4 + nextPolicy.RetryHoldMinutes),
            nextPolicy, [Guid.NewGuid()], "retry", null, null, CancellationToken.None);
        Assert.Equal(8, second.EffectiveHoldMinutes);
        var history = await dir.GetProjectionAsync(checkout, t0.AddMinutes(4), null, CancellationToken.None);
        Assert.Equal(3, history.History.Single(x => x.CycleNumber == 1).EffectiveHoldMinutes);
        Assert.Equal(expires, history.History.Single(x => x.CycleNumber == 1).ExpiresAt);
    }

    [Fact]
    public async Task Multi_line_keeps_shortest_ttl_and_strictest_max()
    {
        await using var catalog = CreateCatalog();
        var offerA = Guid.NewGuid();
        var offerB = Guid.NewGuid();
        var cat = Guid.NewGuid();
        catalog.ReservationCyclePolicyOverrides.Add(ReservationCyclePolicyOverride.Create(
            ReservationCyclePolicyOverride.OfferScope, offerA, 10, 5, 3, DateTimeOffset.UtcNow));
        catalog.ReservationCyclePolicyOverrides.Add(ReservationCyclePolicyOverride.Create(
            ReservationCyclePolicyOverride.OfferScope, offerB, 3, 2, 2, DateTimeOffset.UtcNow));
        await catalog.SaveChangesAsync();
        var snap = await Resolver(catalog, 120, 120, 3).ResolveAsync(
            [new ReservationCyclePolicyLine(offerA, cat), new ReservationCyclePolicyLine(offerB, cat)],
            CancellationToken.None);
        Assert.Equal(3, snap.InitialHoldMinutes);
        Assert.Equal(2, snap.RetryHoldMinutes);
        Assert.Equal(2, snap.MaxCycles);
    }

    [Fact]
    public void Permissions_admin_allowed_seller_denied_without_catalog_permission()
    {
        var endpoints = Read("src/backend/Host/Tooba.Host/Admin/ReservationPolicyAdminEndpoints.cs");
        Assert.Contains("AdminPanelAccess.RequireAuthorizedAsync", endpoints, StringComparison.Ordinal);
        Assert.Contains("SellerPanelAccess.RequireAuthorizedAsync", endpoints, StringComparison.Ordinal);
        Assert.Contains("reservation.policy.seller.denied", endpoints, StringComparison.Ordinal);
        Assert.Contains("فروشنده مجوز تغییر سیاست رزرو ندارد.", endpoints, StringComparison.Ordinal);
        Assert.Equal(ReservationPolicyAdminComposer.SellerMutatePermission, "reservation.policy.mutate");
        var access = Read("src/backend/Modules/AccessControl/Tooba.AccessControl.Domain/AccessControlDomain.cs");
        Assert.DoesNotContain("reservation.policy.mutate", access, StringComparison.Ordinal);
    }

    [Fact]
    public void Ux_and_regression_sources_are_present()
    {
        var settings = Read("src/frontend/app/admin/settings/page.tsx");
        var category = Read("src/frontend/app/admin/category-admin-screen.tsx");
        var editor = Read("src/frontend/app/admin/reservation-policy-editor.tsx");
        var offer = Read("src/frontend/app/admin/offer-reservation-panel.tsx");
        var vendor = Read("src/frontend/app/vendor-panel/products/[offerId]/page.tsx");
        var mapper = Read("src/backend/Modules/Order/Tooba.Order.Application/Admin/Detail/AdminOrderReservationCycleMapper.cs");
        var pending = Read("src/frontend/app/storefront/storefront-pending-payments.tsx");
        Assert.Contains("Inventory reservation policy", editor, StringComparison.Ordinal);
        Assert.Contains("admin-settings-reservation-policy", settings, StringComparison.Ordinal);
        Assert.Contains("admin-settings-save-holds", settings, StringComparison.Ordinal);
        Assert.Contains("admin-settings-cancel-holds", settings, StringComparison.Ordinal);
        Assert.Contains("id: \"reservation\"", category, StringComparison.Ordinal);
        Assert.Contains("inheritLabelFa", editor, StringComparison.Ordinal);
        Assert.Contains("inherited from", editor, StringComparison.Ordinal);
        Assert.Contains("Inherit from store", Read("src/backend/Host/Tooba.Host/Admin/ReservationPolicyAdminComposer.cs"), StringComparison.Ordinal);
        Assert.Contains("overridden", editor, StringComparison.Ordinal);
        Assert.Contains("dir={dir}", editor, StringComparison.Ordinal);
        Assert.Contains("offer-reservation-panel", offer, StringComparison.Ordinal);
        Assert.Contains("canEdit={false}", vendor, StringComparison.Ordinal);
        Assert.Contains("seller-reservation-policy-readonly", vendor, StringComparison.Ordinal);
        Assert.DoesNotContain("InitialReservationHoldMinutes", editor, StringComparison.Ordinal);
        Assert.DoesNotContain("MaxReservationCycles", editor, StringComparison.Ordinal);
        Assert.DoesNotContain("Offer > Category", editor, StringComparison.Ordinal);
        Assert.Contains("ExpiresAt", mapper, StringComparison.Ordinal);
        Assert.Contains("EffectiveHoldMinutes", mapper, StringComparison.Ordinal);
        Assert.Contains("remainingSecondsFromServer", pending, StringComparison.Ordinal);
        Assert.DoesNotContain("TB-P10-T005", Read("src/backend/Host/Tooba.Host/Program.cs"), StringComparison.Ordinal);
        Assert.Contains("MapReservationPolicyAdminEndpoints", Read("src/backend/Host/Tooba.Host/Program.cs"), StringComparison.Ordinal);
        var locks = Read("docs/architecture/TOOBA-LOCKS.md");
        Assert.Contains("LOCK-SF-097", locks, StringComparison.Ordinal);
        Assert.Contains("LOCK-SF-098", locks, StringComparison.Ordinal);
        Assert.Contains("LOCK-SF-099", locks, StringComparison.Ordinal);
        Assert.Contains("LOCK-SF-100", locks, StringComparison.Ordinal);
        Assert.Contains("LOCK-SF-101", locks, StringComparison.Ordinal);
        Assert.Contains("LOCK-SF-102", locks, StringComparison.Ordinal);
    }

    [Fact]
    public void Antipattern_scan_is_clean()
    {
        var editor = Read("src/frontend/app/admin/reservation-policy-editor.tsx");
        var api = Read("src/frontend/app/admin/reservation-policy-api.ts");
        var composer = Read("src/backend/Host/Tooba.Host/Admin/ReservationPolicyAdminComposer.cs");
        var endpoints = Read("src/backend/Host/Tooba.Host/Admin/ReservationPolicyAdminEndpoints.cs");
        var resolver = Read("src/backend/Modules/Order/Tooba.Order.Application/ReservationCycle/Policies/ReservationCyclePolicyResolver.cs");
        Assert.DoesNotContain("Offer > Category > Store", editor, StringComparison.Ordinal);
        Assert.DoesNotContain("product-level", composer, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PreviewAsync", resolver, StringComparison.Ordinal);
        Assert.Contains("IReservationCycleHoldPolicyReader", resolver, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogDbContext", resolver, StringComparison.Ordinal);
        Assert.Contains("reservation.policy.seller.denied", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("ExpiresAt =", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("setInterval", editor, StringComparison.Ordinal);
        Assert.DoesNotContain("setInterval", api, StringComparison.Ordinal);
        Assert.Contains("false,", composer, StringComparison.Ordinal);
        Assert.Contains("SellerCanMutate", Read("src/backend/Host/Tooba.Host/Admin/ReservationPolicyAdminModels.cs"), StringComparison.Ordinal);
        Assert.DoesNotContain("ReservationCyclePolicyResolver concrete", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("is Tooba.Host.ReservationCyclePolicyResolver", endpoints, StringComparison.Ordinal);
    }

    private static ReservationCyclePolicyResolver Resolver(
        CatalogDbContext catalog,
        int initial,
        int retry,
        int max) =>
        new(
            Options.Create(new ReservationCycleOptions
            {
                InitialReservationHoldMinutes = initial,
                RetryReservationHoldMinutes = retry,
                MaxReservationCycles = max,
            }),
            new Tooba.Catalog.Infrastructure.Reservation.ReservationCycleHoldPolicyReader(catalog));

    private static CatalogDbContext CreateCatalog()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new CatalogDbContext(options);
    }

    private static OrderDbContext CreateOrders()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new OrderDbContext(options);
    }

    private static string Read(string relative)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, relative);
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException(relative);
    }
}
