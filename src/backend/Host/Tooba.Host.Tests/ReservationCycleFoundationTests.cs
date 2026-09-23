using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
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

/// <summary>TB-P10-T004-R15 — چرخه رزرو متمایز از PaymentAttempt.</summary>
public sealed class ReservationCycleFoundationTests
{
    [Fact]
    public void Domain_starts_cycle_one_and_forbids_rewrite()
    {
        var now = DateTimeOffset.Parse("2026-09-13T10:00:00Z");
        var cycle = ReservationCycle.Start(
            Guid.NewGuid(),
            1,
            ReservationCycleReason.InitialPayment,
            now,
            now.AddMinutes(10),
            10,
            3,
            "platform",
            [Guid.NewGuid()],
            "commit",
            "cycle:1",
            null);
        Assert.Equal(1, cycle.CycleNumber);
        Assert.Equal(ReservationCycleStatus.Active, cycle.Status);
        var expires = cycle.ExpiresAt;
        cycle.CorrelatePaymentAttempt(Guid.NewGuid());
        Assert.Equal(expires, cycle.ExpiresAt);
        cycle.Close(ReservationCycleStatus.Expired, now.AddMinutes(10));
        Assert.Throws<InvalidOperationException>(() => cycle.CorrelatePaymentAttempt(Guid.NewGuid()));
        Assert.Throws<InvalidOperationException>(() => cycle.Close(ReservationCycleStatus.CommittedPaid, now));
    }

    [Fact]
    public void Manual_review_stays_same_cycle_number()
    {
        var now = DateTimeOffset.Parse("2026-09-13T10:00:00Z");
        var cycle = ReservationCycle.Start(
            Guid.NewGuid(), 1, ReservationCycleReason.ManualInitial, now, now.AddMinutes(10), 10, 3, "store",
            [Guid.NewGuid()], null, null, null);
        cycle.TransitionManualReview(now.AddHours(24), now.AddMinutes(3));
        Assert.Equal(1, cycle.CycleNumber);
        Assert.Equal(ReservationCycleReason.ManualReview, cycle.Reason);
        Assert.Equal(now.AddHours(24), cycle.ExpiresAt);
        Assert.Equal(ReservationCycleStatus.Active, cycle.Status);
    }

    [Fact]
    public async Task Directory_initial_retry_max_and_failed_reacquire()
    {
        await using var db = CreateOrderDb();
        var dir = new ReservationCycleDirectory(db);
        var checkout = Guid.NewGuid();
        var policy = new ReservationCyclePolicySnapshot(10, 5, 3, "platform");
        var t0 = DateTimeOffset.Parse("2026-09-13T10:00:00Z");
        var first = await dir.StartAsync(
            checkout, ReservationCycleReason.InitialPayment, t0, t0.AddMinutes(10), policy,
            [Guid.NewGuid()], "commit", null, null, CancellationToken.None);
        Assert.Equal(1, first.CycleNumber);
        Assert.Equal(10, first.EffectiveHoldMinutes);

        await dir.CorrelatePaymentAttemptAsync(checkout, Guid.NewGuid(), CancellationToken.None);
        var still = await dir.GetActiveAsync(checkout, CancellationToken.None);
        Assert.Equal(first.ExpiresAt, still!.ExpiresAt);
        Assert.Equal(1, still.CycleNumber);

        await dir.CloseExpiredDueAsync(t0.AddMinutes(10), CancellationToken.None);
        var closed = await dir.GetActiveAsync(checkout, CancellationToken.None);
        Assert.Null(closed);
        var events = await dir.ListEventsAsync(checkout, CancellationToken.None);
        Assert.Contains(events, x => x.Kind == ReservationCycleEventKind.Expired);

        var second = await dir.StartAsync(
            checkout, ReservationCycleReason.RetryAfterExpiry, t0.AddMinutes(11), t0.AddMinutes(16), policy,
            [Guid.NewGuid()], "retry", null, null, CancellationToken.None);
        Assert.Equal(2, second.CycleNumber);
        Assert.Equal(5, second.EffectiveHoldMinutes);
        Assert.Equal(1, first.CycleNumber);
        Assert.Equal(ReservationCycleStatus.Expired, (await dir.GetProjectionAsync(checkout, t0.AddMinutes(11), "AvailableForReacquire", CancellationToken.None))
            .History.Single(x => x.CycleNumber == 1).Status);

        await dir.CloseActiveAsync(checkout, ReservationCycleStatus.Expired, t0.AddMinutes(16), CancellationToken.None);
        var third = await dir.StartAsync(
            checkout, ReservationCycleReason.RetryAfterExpiry, t0.AddMinutes(17), t0.AddMinutes(22), policy,
            [Guid.NewGuid()], "retry", null, null, CancellationToken.None);
        Assert.Equal(3, third.CycleNumber);
        await dir.CloseActiveAsync(checkout, ReservationCycleStatus.Expired, t0.AddMinutes(22), CancellationToken.None);
        Assert.Equal(3, await dir.CountCreatedAsync(checkout, CancellationToken.None));

        await dir.RecordReacquireRequestedAsync(checkout, t0.AddMinutes(23), CancellationToken.None);
        await dir.RecordReacquireFailedAsync(checkout, t0.AddMinutes(23), "Unavailable", CancellationToken.None);
        Assert.Equal(3, await dir.CountCreatedAsync(checkout, CancellationToken.None));
        Assert.Null(await dir.GetActiveAsync(checkout, CancellationToken.None));

        await dir.RecordRetryLimitReachedAsync(checkout, t0.AddMinutes(24), CancellationToken.None);
        events = await dir.ListEventsAsync(checkout, CancellationToken.None);
        Assert.Contains(events, x => x.Kind == ReservationCycleEventKind.RetryLimitReached);
        Assert.Contains(events, x => x.Kind == ReservationCycleEventKind.ReacquireFailed);
        Assert.DoesNotContain(events, x => x.Kind == ReservationCycleEventKind.ReacquireFailed && x.CycleId is not null);
    }

    [Fact]
    public async Task Directory_double_start_and_success_cancel_are_safe()
    {
        await using var db = CreateOrderDb();
        var dir = new ReservationCycleDirectory(db);
        var checkout = Guid.NewGuid();
        var policy = new ReservationCyclePolicySnapshot(10, 5, 3, "offer");
        var now = DateTimeOffset.Parse("2026-09-13T10:00:00Z");
        var a = await dir.StartAsync(
            checkout, ReservationCycleReason.InitialPayment, now, now.AddMinutes(10), policy,
            [Guid.NewGuid()], null, $"cycle:{checkout:N}:1", null, CancellationToken.None);
        var b = await dir.StartAsync(
            checkout, ReservationCycleReason.InitialPayment, now, now.AddMinutes(10), policy,
            [Guid.NewGuid()], null, $"cycle:{checkout:N}:1", null, CancellationToken.None);
        Assert.Equal(a.CycleId, b.CycleId);
        Assert.Equal(1, await dir.CountCreatedAsync(checkout, CancellationToken.None));

        await dir.CloseActiveAsync(checkout, ReservationCycleStatus.CommittedPaid, now.AddMinutes(2), CancellationToken.None);
        Assert.Null(await dir.GetActiveAsync(checkout, CancellationToken.None));
        var paid = (await dir.GetProjectionAsync(checkout, now.AddMinutes(2), "Reserved", CancellationToken.None)).History.Single();
        Assert.Equal(ReservationCycleStatus.CommittedPaid, paid.Status);

        var other = Guid.NewGuid();
        await dir.StartAsync(
            other, ReservationCycleReason.InitialPayment, now, now.AddMinutes(10), policy,
            [Guid.NewGuid()], null, null, null, CancellationToken.None);
        await dir.CloseActiveAsync(other, ReservationCycleStatus.ReleasedByCancel, now.AddMinutes(1), CancellationToken.None);
        Assert.Equal(
            ReservationCycleStatus.ReleasedByCancel,
            (await dir.GetProjectionAsync(other, now.AddMinutes(1), null, CancellationToken.None)).History.Single().Status);
    }

    [Fact]
    public async Task Projection_is_side_effect_free_and_counts_remaining()
    {
        await using var db = CreateOrderDb();
        var dir = new ReservationCycleDirectory(db);
        var checkout = Guid.NewGuid();
        var now = DateTimeOffset.Parse("2026-09-13T10:00:00Z");
        await dir.StartAsync(
            checkout, ReservationCycleReason.InitialPayment, now, now.AddMinutes(10),
            new ReservationCyclePolicySnapshot(10, 5, 3, "platform"),
            [Guid.NewGuid()], null, null, null, CancellationToken.None);
        var before = await dir.CountCreatedAsync(checkout, CancellationToken.None);
        var projection = await dir.GetProjectionAsync(checkout, now.AddMinutes(3), "Reserved", CancellationToken.None);
        Assert.Equal(before, await dir.CountCreatedAsync(checkout, CancellationToken.None));
        Assert.Equal(1, projection.CurrentCycleNumber);
        Assert.Equal(ReservationCycleStatus.Active, projection.CurrentStatus);
        Assert.Equal(420, projection.SecondsRemaining);
        Assert.Equal(2, projection.RetryCountRemaining);
        Assert.Equal(now.AddMinutes(3), projection.ServerTime);
        Assert.Equal("Reserved", projection.SupplyStatus);
    }

    [Fact]
    public async Task Policy_uses_offer_over_category_and_minimum_across_lines()
    {
        await using var catalog = CreateCatalogDb();
        catalog.StoreHoldPolicySettings.Add(StoreHoldPolicySettings.CreateDefault(DateTimeOffset.UtcNow));
        var offerA = Guid.NewGuid();
        var offerB = Guid.NewGuid();
        var cat = Guid.NewGuid();
        catalog.ReservationCyclePolicyOverrides.Add(ReservationCyclePolicyOverride.Create(
            ReservationCyclePolicyOverride.OfferScope, offerA, 10, 5, 3, DateTimeOffset.UtcNow));
        catalog.ReservationCyclePolicyOverrides.Add(ReservationCyclePolicyOverride.Create(
            ReservationCyclePolicyOverride.OfferScope, offerB, 3, 2, 2, DateTimeOffset.UtcNow));
        catalog.ReservationCyclePolicyOverrides.Add(ReservationCyclePolicyOverride.Create(
            ReservationCyclePolicyOverride.CategoryScope, cat, 30, 20, 8, DateTimeOffset.UtcNow));
        await catalog.SaveChangesAsync();
        var resolver = new ReservationCyclePolicyResolver(
            Options.Create(new ReservationCycleOptions
            {
                InitialReservationHoldMinutes = 120,
                RetryReservationHoldMinutes = 120,
                MaxReservationCycles = 3,
            }),
            new Tooba.Catalog.Infrastructure.Reservation.ReservationCycleHoldPolicyReader(catalog));
        var snap = await resolver.ResolveAsync(
            [
                new ReservationCyclePolicyLine(offerA, cat),
                new ReservationCyclePolicyLine(offerB, cat),
            ],
            CancellationToken.None);
        Assert.Equal(3, snap.InitialHoldMinutes);
        Assert.Equal(2, snap.RetryHoldMinutes);
        Assert.Equal(2, snap.MaxCycles);
    }

    [Fact]
    public void Decimal_quantity_is_numeric_not_float()
    {
        var now = DateTimeOffset.UtcNow;
        var hold = Tooba.Inventory.Domain.Aggregates.StockReservation.Hold(Guid.NewGuid(), Guid.NewGuid(), 1.25m, "order", null, now, now.AddMinutes(10));
        Assert.Equal(1.25m, hold.Quantity);
        Assert.Equal(0, 1.25m.CompareTo(hold.Quantity));
    }

    [Fact]
    public void Antipattern_scan_is_clean()
    {
        var dir = Read("src/backend/Modules/Order/Tooba.Order.Infrastructure/ReservationCycleDirectory.cs");
        var checkout = Read("src/backend/Modules/Order/Tooba.Order.Infrastructure/CheckoutDirectory.cs");
        var composer = Read("src/backend/Modules/Payment/Tooba.Payment.Application/Orchestration/StorefrontPaymentOrchestrator.cs");
        var cart = Read("src/backend/Modules/Cart/Tooba.Cart.Infrastructure/Directories/CartDirectory.cs");
        Assert.Contains("CorrelatePaymentAttempt", dir, StringComparison.Ordinal);
        Assert.Contains("PrepareStart", checkout, StringComparison.Ordinal);
        Assert.Contains("EnsureRetrySupplyAsync", composer, StringComparison.Ordinal);
        Assert.Contains("EnsureRetryAfterExpiryAsync", Read("src/backend/Modules/Order/Tooba.Order.Application/ReservationCycle/Services/ReservationCycleCoordinator.cs"), StringComparison.Ordinal);
        Assert.DoesNotContain("ReserveAsync", cart, StringComparison.Ordinal);
        Assert.DoesNotContain("TB-P10-T005", Read("src/backend/Host/Tooba.Host/Program.cs"), StringComparison.Ordinal);
        Assert.Contains("InitialReservationHoldMinutes", Read("src/backend/Host/Tooba.Host/appsettings.json"), StringComparison.Ordinal);
        Assert.Contains("inventory.reservation.retry_limit_reached", Read("src/backend/Modules/Order/Tooba.Order.Application/ReservationCycle/Contracts/ReservationCycleContracts.cs"), StringComparison.Ordinal);
        Assert.DoesNotContain("setInterval", dir, StringComparison.Ordinal);
        Assert.DoesNotContain("setInterval", Read("src/backend/Modules/Order/Tooba.Order.Application/ReservationCycle/Services/ReservationCycleCoordinator.cs"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Directory_batches_projections_without_changing_semantics()
    {
        await using var db = CreateOrderDb();
        var dir = new ReservationCycleDirectory(db);
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var policy = new ReservationCyclePolicySnapshot(10, 5, 3, "platform");
        var t0 = DateTimeOffset.Parse("2026-09-13T10:00:00Z");
        await dir.StartAsync(first, ReservationCycleReason.InitialPayment, t0, t0.AddMinutes(10), policy, [Guid.NewGuid()], "commit", null, null, CancellationToken.None);
        await dir.StartAsync(second, ReservationCycleReason.InitialPayment, t0, t0.AddMinutes(5), policy, [Guid.NewGuid()], "commit", null, null, CancellationToken.None);
        var map = await dir.GetProjectionsAsync([first, second], t0.AddMinutes(1), null, CancellationToken.None);
        Assert.Equal(2, map.Count);
        Assert.Equal(1, map[first].CurrentCycleNumber);
        Assert.Equal(540, map[first].SecondsRemaining);
        Assert.Equal(240, map[second].SecondsRemaining);
        var single = await dir.GetProjectionAsync(first, t0.AddMinutes(1), null, CancellationToken.None);
        Assert.Equal(map[first].SecondsRemaining, single.SecondsRemaining);
    }

    [Fact]
    public void Coordinator_and_locks_are_wired()
    {
        Assert.DoesNotContain("ReservationCycleCoordinator", Read("src/backend/Host/Tooba.Host/Program.cs"), StringComparison.Ordinal);
        Assert.Contains("IReservationCyclePolicyResolver", Read("src/backend/Modules/Order/Tooba.Order.Infrastructure/OrderModule.cs"), StringComparison.Ordinal);
        Assert.Contains("IReservationCycleCoordinator", Read("src/backend/Modules/Order/Tooba.Order.Infrastructure/OrderModule.cs"), StringComparison.Ordinal);
        Assert.Contains("IUnpaidOrderExpiryReconciler", Read("src/backend/Host/Tooba.Host/UnpaidOrderExpiryHostedService.cs"), StringComparison.Ordinal);
        Assert.DoesNotContain("IReservationCycleDirectory", Read("src/backend/Host/Tooba.Host/UnpaidOrderExpiryHostedService.cs"), StringComparison.Ordinal);
        Assert.DoesNotContain("DateTimeOffset.UtcNow", Read("src/backend/Host/Tooba.Host/UnpaidOrderExpiryHostedService.cs"), StringComparison.Ordinal);
        Assert.Contains("LOCK-SF-076", Read("docs/architecture/TOOBA-LOCKS.md"), StringComparison.Ordinal);
        Assert.Contains("Reservation Cycle is not a Payment Attempt", Read("docs/architecture/TOOBA-LOCKS.md"), StringComparison.Ordinal);
    }

    private static OrderDbContext CreateOrderDb()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new OrderDbContext(options);
    }

    private static CatalogDbContext CreateCatalogDb()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new CatalogDbContext(options);
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
