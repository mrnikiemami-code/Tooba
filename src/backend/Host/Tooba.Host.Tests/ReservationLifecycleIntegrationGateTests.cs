using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Host.Admin;
using Tooba.Host.Storefront;
using Tooba.Order.Application.Storefront.Services;
using Tooba.Order.Application.Storefront.Models;
using Tooba.AddressBook.Contracts;
using Tooba.Cart.Application.Ports;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Order.Application;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Payment.Domain.Aggregates;
using Tooba.Payment.Domain.ValueObjects;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-P10-T004-R19 — دروازه یکپارچه‌سازی نهایی چرخه رزرو.</summary>
public sealed class ReservationLifecycleIntegrationGateTests
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.Parse("2026-09-13T10:00:00Z");

    [Fact]
    public async Task Flash_sale_mixed_policy_uses_three_then_one_and_blocks_cycle_three()
    {
        await using var catalog = CreateCatalog();
        var store = StoreHoldPolicySettings.CreateDefault(T0);
        store.ReplaceReservationCycle(10, 5, 3, T0);
        catalog.StoreHoldPolicySettings.Add(store);
        var offerA = Guid.NewGuid();
        var offerB = Guid.NewGuid();
        catalog.ReservationCyclePolicyOverrides.Add(ReservationCyclePolicyOverride.Create(
            ReservationCyclePolicyOverride.OfferScope, offerA, 3, 1, 2, T0));
        await catalog.SaveChangesAsync();

        var policy = await Resolver(catalog, 120, 120, 3).ResolveAsync(
            [new ReservationCyclePolicyLine(offerA, Guid.NewGuid()), new ReservationCyclePolicyLine(offerB, Guid.NewGuid())],
            CancellationToken.None);
        Assert.Equal(3, policy.InitialHoldMinutes);
        Assert.Equal(1, policy.RetryHoldMinutes);
        Assert.Equal(2, policy.MaxCycles);

        await using var orders = CreateOrders();
        var dir = new ReservationCycleDirectory(orders);
        var checkout = Guid.NewGuid();
        var first = await dir.StartAsync(
            checkout, ReservationCycleReason.InitialPayment, T0, T0.AddMinutes(policy.InitialHoldMinutes), policy,
            [Guid.NewGuid()], "commit", $"cycle:{checkout:N}:1", null, CancellationToken.None);
        Assert.Equal(1, first.CycleNumber);
        Assert.Equal(3, first.EffectiveHoldMinutes);
        var expires1 = first.ExpiresAt;

        await dir.CorrelatePaymentAttemptAsync(checkout, Guid.NewGuid(), CancellationToken.None);
        var still = await dir.GetActiveAsync(checkout, CancellationToken.None);
        Assert.Equal(expires1, still!.ExpiresAt);
        Assert.Equal(1, still.CycleNumber);

        await dir.CloseExpiredDueAsync(T0.AddMinutes(3), CancellationToken.None);
        Assert.Null(await dir.GetActiveAsync(checkout, CancellationToken.None));
        Assert.Equal(1, await dir.CountCreatedAsync(checkout, CancellationToken.None));

        var second = await dir.StartAsync(
            checkout, ReservationCycleReason.RetryAfterExpiry, T0.AddMinutes(3),
            T0.AddMinutes(3).AddMinutes(policy.RetryHoldMinutes), policy,
            [Guid.NewGuid()], "retry", $"cycle:{checkout:N}:2", null, CancellationToken.None);
        Assert.Equal(2, second.CycleNumber);
        Assert.Equal(1, second.EffectiveHoldMinutes);
        Assert.Equal(3, first.EffectiveHoldMinutes);

        await dir.CloseExpiredDueAsync(T0.AddMinutes(4), CancellationToken.None);
        Assert.Equal(2, await dir.CountCreatedAsync(checkout, CancellationToken.None));
        Assert.True(await dir.CountCreatedAsync(checkout, CancellationToken.None) >= policy.MaxCycles);
        await dir.RecordRetryLimitReachedAsync(checkout, T0.AddMinutes(5), CancellationToken.None);
        Assert.Null(await dir.GetActiveAsync(checkout, CancellationToken.None));
        Assert.Equal(2, await dir.CountCreatedAsync(checkout, CancellationToken.None));
    }

    [Fact]
    public async Task Precedence_offer_category_store_platform_and_settings_future_only()
    {
        await using var catalog = CreateCatalog();
        var store = StoreHoldPolicySettings.CreateDefault(T0);
        store.ReplaceReservationCycle(10, 5, 3, T0);
        catalog.StoreHoldPolicySettings.Add(store);
        var category = Guid.NewGuid();
        var offer = Guid.NewGuid();
        catalog.ReservationCyclePolicyOverrides.Add(ReservationCyclePolicyOverride.Create(
            ReservationCyclePolicyOverride.CategoryScope, category, 8, 4, 2, T0));
        catalog.ReservationCyclePolicyOverrides.Add(ReservationCyclePolicyOverride.Create(
            ReservationCyclePolicyOverride.OfferScope, offer, 3, null, null, T0));
        await catalog.SaveChangesAsync();
        var resolver = Resolver(catalog, 120, 90, 6);

        var platformOnly = await resolver.ResolveAsync([], CancellationToken.None);
        Assert.Equal(10, platformOnly.InitialHoldMinutes);

        var categoryLine = await resolver.ResolveAsync(
            [new ReservationCyclePolicyLine(Guid.NewGuid(), category)], CancellationToken.None);
        Assert.Equal(8, categoryLine.InitialHoldMinutes);
        Assert.Equal(4, categoryLine.RetryHoldMinutes);
        Assert.Equal(2, categoryLine.MaxCycles);

        var offerLine = await resolver.ResolveAsync(
            [new ReservationCyclePolicyLine(offer, category)], CancellationToken.None);
        Assert.Equal(3, offerLine.InitialHoldMinutes);
        Assert.Equal(4, offerLine.RetryHoldMinutes);
        Assert.Equal(2, offerLine.MaxCycles);

        await using var orders = CreateOrders();
        var dir = new ReservationCycleDirectory(orders);
        var checkout = Guid.NewGuid();
        var first = await dir.StartAsync(
            checkout, ReservationCycleReason.InitialPayment, T0, T0.AddMinutes(3), offerLine,
            [Guid.NewGuid()], "commit", null, null, CancellationToken.None);
        var expires = first.ExpiresAt;
        ReservationPolicyAdminComposer.ReplaceStore(
            store, new ReservationPolicyWriteRequest(20, 15, 5), Guid.NewGuid(), T0.AddMinutes(1), catalog);
        await catalog.SaveChangesAsync();
        Assert.Equal(expires, (await dir.GetActiveAsync(checkout, CancellationToken.None))!.ExpiresAt);
        var next = await resolver.ResolveAsync([], CancellationToken.None);
        Assert.Equal(20, next.InitialHoldMinutes);
        Assert.Equal(15, next.RetryHoldMinutes);
    }

    [Fact]
    public void Lifecycle_matrix_customer_and_admin_ctas_are_consistent()
    {
        var checkout = Guid.NewGuid();
        var pending = StorefrontPendingPaymentProjector.Project(
            [Checkout(checkout)],
            Pay(checkout, "Pending", "sandbox"),
            Cycle(checkout, 1, ReservationCycleStatus.Active, 180, created: 1, max: 2, remaining: 1, expires: T0.AddMinutes(3)),
            T0).Items.Single();
        Assert.Equal("pay", pending.PrimaryAction);
        Assert.False(pending.CanRetryPayment);

        var failed = StorefrontPendingPaymentProjector.Project(
            [Checkout(checkout)],
            Pay(checkout, "Failed", "sandbox"),
            Cycle(checkout, 1, ReservationCycleStatus.Active, 180, created: 1, max: 2, remaining: 1, expires: T0.AddMinutes(3)),
            T0).Items.Single();
        Assert.Equal(pending.HoldEndsAt, failed.HoldEndsAt);
        Assert.Equal("pay", failed.PrimaryAction);

        var expired = StorefrontPendingPaymentProjector.Project(
            [Checkout(checkout)],
            Pay(checkout, "Expired", "sandbox"),
            Cycle(checkout, 1, ReservationCycleStatus.Expired, 0, created: 1, max: 2, remaining: 1, expires: T0.AddMinutes(-1)),
            T0).Items.Single();
        Assert.Equal("retryAfterExpiry", expired.PrimaryAction);

        var maxed = StorefrontPendingPaymentProjector.Project(
            [Checkout(checkout)],
            Pay(checkout, "Expired", "sandbox"),
            Cycle(checkout, 2, ReservationCycleStatus.Expired, 0, created: 2, max: 2, remaining: 0),
            T0).Items.Single();
        Assert.Equal("none", maxed.PrimaryAction);
        Assert.True(maxed.HasReachedRetryLimit);

        var manual = StorefrontPendingPaymentProjector.Project(
            [Checkout(checkout)],
            new Dictionary<Guid, StorefrontPendingPaymentProjector.PaymentInput>
            {
                [checkout] = new(Guid.NewGuid(), "Pending", "manual", T0.AddMinutes(-5), 1000, "IRR"),
            },
            Cycle(checkout, 1, ReservationCycleStatus.Active, 400, created: 1, max: 2, remaining: 1, expires: T0.AddHours(24)),
            T0).Items.Single();
        Assert.True(manual.IsManualAwaitingReview);
        Assert.Equal("none", manual.PrimaryAction);

        var paid = StorefrontPendingPaymentProjector.Project(
            [Checkout(checkout)],
            Pay(checkout, "Succeeded", "sandbox"),
            Cycle(checkout, 1, ReservationCycleStatus.CommittedPaid, 0, created: 1, max: 2, remaining: 1),
            T0);
        Assert.Empty(paid.Items);

        var history = new ReservationCycleSnapshot(
            Guid.NewGuid(), checkout, 1, ReservationCycleReason.InitialPayment, ReservationCycleStatus.Active,
            T0, T0.AddMinutes(3), null, 3, 2, "offer", [], null, null);
        var admin = AdminReservationCycleMapper.ToAudit(
            new ReservationCycleProjection(
                checkout, 1, ReservationCycleStatus.Active, T0, T0.AddMinutes(3), T0, 180, 1, 2, 1,
                [history], "Reserved"),
            [],
            null);
        Assert.False(admin.CanExtendTimer);
        Assert.False(admin.CanRetryReservation);
    }

    [Fact]
    public void Manual_review_stays_same_cycle_and_decimal_quantity_is_exact()
    {
        var cycle = ReservationCycle.Start(
            Guid.NewGuid(), 1, ReservationCycleReason.ManualInitial, T0, T0.AddMinutes(3), 3, 2, "offer",
            [Guid.NewGuid()], null, null, null);
        cycle.TransitionManualReview(T0.AddHours(24), T0.AddMinutes(1));
        Assert.Equal(1, cycle.CycleNumber);
        Assert.Equal(ReservationCycleReason.ManualReview, cycle.Reason);
        Assert.Equal(T0.AddHours(24), cycle.ExpiresAt);
        var hold = Tooba.Inventory.Domain.Aggregates.StockReservation.Hold(Guid.NewGuid(), Guid.NewGuid(), 1.25m, "order", null, T0, T0.AddMinutes(3));
        Assert.Equal(0, 1.25m.CompareTo(hold.Quantity));
    }

    [Fact]
    public void Commit_hold_uses_category_and_gate_sources_are_clean()
    {
        var checkout = Read("src/backend/Modules/Order/Tooba.Order.Infrastructure/CheckoutDirectory.cs");
        Assert.Contains("GetPrimaryCategoryIdsByVariantIdsAsync", checkout, StringComparison.Ordinal);
        Assert.Contains("ResolveInitialCycleExpiresAtAsync", checkout, StringComparison.Ordinal);
        Assert.DoesNotContain("new ReservationCyclePolicyLine(x.OfferId, null)", checkout, StringComparison.Ordinal);
        Assert.Contains("CategoryIdSnapshot", checkout, StringComparison.Ordinal);

        var coordinator = Read("src/backend/Host/Tooba.Host/ReservationCycleCoordinator.cs");
        Assert.Contains("allowReacquire: false", coordinator, StringComparison.Ordinal);
        Assert.Contains("RetryHoldMinutes", coordinator, StringComparison.Ordinal);
        Assert.Contains("RetryLimitReachedFa", coordinator, StringComparison.Ordinal);

        var pendingUi = Read("src/frontend/app/storefront/storefront-pending-payments.tsx");
        Assert.Contains("remainingSecondsFromServer", pendingUi, StringComparison.Ordinal);
        Assert.Contains("shouldRefreshOnceAtZero", pendingUi, StringComparison.Ordinal);
        Assert.DoesNotContain("setInterval(() => fetch", pendingUi, StringComparison.Ordinal);

        var editor = Read("src/frontend/app/admin/reservation-policy-editor.tsx");
        Assert.DoesNotContain("Offer > Category", editor, StringComparison.Ordinal);
        Assert.Contains("dir={dir}", editor, StringComparison.Ordinal);

        var mapper = Read("src/backend/Host/Tooba.Host/Admin/AdminReservationCycleMapper.cs");
        Assert.Contains("CanExtendTimer", mapper, StringComparison.Ordinal);
        Assert.Contains("false", mapper, StringComparison.Ordinal);

        Assert.DoesNotContain("TB-P10-T005", Read("src/backend/Host/Tooba.Host/Program.cs"), StringComparison.Ordinal);
        Assert.Contains("LOCK-SF-102", Read("docs/architecture/TOOBA-LOCKS.md"), StringComparison.Ordinal);
        Assert.Contains("InitialReservationHoldMinutes", Read("src/backend/Host/Tooba.Host/appsettings.json"), StringComparison.Ordinal);
        Assert.Contains("AddReservationCycles", Read("src/backend/Modules/Order/Tooba.Order.Infrastructure/Persistence/Migrations/20260913033000_AddReservationCycles.cs"), StringComparison.Ordinal);
        Assert.Contains("AddReservationCyclePolicy", Read("src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Persistence/Migrations/20260913033100_AddReservationCyclePolicy.cs"), StringComparison.Ordinal);
        Assert.Contains("AddReservationPolicyAudit", Read("src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Persistence/Migrations/20260913080000_AddReservationPolicyAudit.cs"), StringComparison.Ordinal);
    }

    private static ReservationCyclePolicyResolver Resolver(CatalogDbContext catalog, int initial, int retry, int max) =>
        new(
            Options.Create(new ReservationCycleOptions
            {
                InitialReservationHoldMinutes = initial,
                RetryReservationHoldMinutes = retry,
                MaxReservationCycles = max,
            }),
            catalog);

    private static CatalogDbContext CreateCatalog() =>
        new(new DbContextOptionsBuilder<CatalogDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);

    private static OrderDbContext CreateOrders() =>
        new(new DbContextOptionsBuilder<OrderDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);

    private static StorefrontPendingPaymentProjector.CheckoutInput Checkout(Guid id) =>
        new(id, Guid.NewGuid(), Guid.NewGuid(), T0,
            [new("SO-R19", SellerOrderStatus.PendingPayment, 250000, "IRR", [new("کالا", 1, Guid.NewGuid())])]);

    private static Dictionary<Guid, StorefrontPendingPaymentProjector.PaymentInput> Pay(
        Guid checkout, string status, string provider) =>
        new() { [checkout] = new(Guid.NewGuid(), status, provider, null, 250000, "IRR") };

    private static Dictionary<Guid, ReservationCycleProjection> Cycle(
        Guid checkout,
        int number,
        ReservationCycleStatus status,
        int seconds,
        int created = 1,
        int max = 3,
        int remaining = 2,
        DateTimeOffset? expires = null) =>
        new()
        {
            [checkout] = new ReservationCycleProjection(
                checkout,
                number,
                status,
                T0.AddMinutes(-5),
                expires,
                T0,
                seconds,
                created,
                max,
                remaining,
                [],
                null),
        };

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
