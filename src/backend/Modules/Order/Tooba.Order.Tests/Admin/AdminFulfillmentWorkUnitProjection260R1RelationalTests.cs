using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.Fulfillment.Application.Fulfillments.Ports;
using Tooba.Fulfillment.Application.WorkQueue.Models;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Domain.ValueObjects;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Fulfillment.Infrastructure.Queries;
using Tooba.Order.Domain.Checkout;
using Tooba.Order.Infrastructure.Integrations.DispatchPlan;
using Tooba.Order.Infrastructure.Integrations.GridEnrichment;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Party.Contracts.Ports;
using Tooba.Persistence;
using Xunit;
using Xunit.Abstractions;

namespace Tooba.Order.Tests.Admin;

/// <summary>
/// TB-260-R1 on REAL storage: the admin work queue must project ONE row per PHYSICAL work unit (unallocated
/// accepted group / independent parcel / active common package), each bound to its OWN accepted dispatch plan and
/// its OWN exact locator — never one seller-summary row that lends the earliest plan to a sibling.
/// </summary>
/// <remarks>
/// Gated by <c>TB260R1_CONNECTION</c>: with no target nothing runs and nothing is claimed. Fixtures are created with
/// the production owning paths only (accepted-evidence materializer for Order; paid-order unit + real parcel/package
/// domain methods for Fulfillment) and are task-labelled (<c>TB260R1-…</c>). Real user rows are never read/written.
/// </remarks>
public sealed class AdminFulfillmentWorkUnitProjection260R1RelationalTests
{
    private const string RelationalEnvVar = "TB260R1_CONNECTION";

    private readonly ITestOutputHelper _output;

    public AdminFulfillmentWorkUnitProjection260R1RelationalTests(ITestOutputHelper output) => _output = output;

    private static string? Connection =>
        Environment.GetEnvironmentVariable(RelationalEnvVar) is { Length: > 0 } raw ? raw : null;

    // ---------------------------------------------------------------- A / A2

    [Fact]
    public async Task Case_A_one_seller_two_groups_keep_their_own_plan_state_and_exact_locator()
    {
        if (Connection is null)
        {
            return;
        }

        var target = CanonicalTarget();
        var now = DateTimeOffset.UtcNow;
        var carrierToday = AdminFulfillmentDispatchTimingAuthority.LocalDate(now, "Asia/Tehran");
        var tag = $"TB260R1-A-{DateTime.UtcNow:HHmmss}";

        var checkoutId = Guid.NewGuid();
        var sellerA = Guid.NewGuid();
        var sellerOrder = Guid.NewGuid();
        var lineA = Guid.NewGuid();
        var lineB = Guid.NewGuid();
        var cartLineA = Guid.NewGuid();
        var cartLineB = Guid.NewGuid();
        var groupToday = Guid.NewGuid();
        var groupTomorrow = Guid.NewGuid();

        await using var orderDb = NewOrderDb(target);
        await using var fulfillmentDb = NewFulfillmentDb(target);

        await InsertCheckoutAsync(orderDb, checkoutId, tag, now);
        await InsertSellerOrderAsync(orderDb, sellerOrder, checkoutId, $"{tag}-1", sellerA);
        await InsertOrderLineAsync(orderDb, lineA, sellerOrder, sellerA);
        await InsertOrderLineAsync(orderDb, lineB, sellerOrder, sellerA);

        var todayPlan = ShippingGroupPlannedDispatch.Create(
            carrierToday,
            new TimeOnly(0, 0).Ticks,
            new TimeOnly(23, 59, 59).Ticks,
            "Asia/Tehran",
            transportWorkingDays: 1,
            policyVersion: 1,
            scheduleSnapshotId: Guid.NewGuid(),
            scheduleSourceRevision: 7);
        var tomorrowPlan = ShippingGroupPlannedDispatch.Create(
            carrierToday.AddDays(1),
            new TimeOnly(0, 0).Ticks,
            new TimeOnly(23, 59, 59).Ticks,
            "Asia/Tehran",
            transportWorkingDays: 1,
            policyVersion: 1,
            scheduleSnapshotId: Guid.NewGuid(),
            scheduleSourceRevision: 7);

        var materialized = OrderShippingGroupMaterializer.Materialize(
            [
                Plan(groupToday, sellerA, cartLineA, Evidence(carrierToday, todayPlan), carrierToday),
                Plan(groupTomorrow, sellerA, cartLineB, Evidence(carrierToday.AddDays(1), tomorrowPlan), carrierToday.AddDays(1)),
            ],
            [(lineA, sellerA), (lineB, sellerA)],
            new Dictionary<Guid, Guid> { [sellerA] = sellerOrder },
            new Dictionary<Guid, Guid> { [cartLineA] = lineA, [cartLineB] = lineB },
            checkoutId,
            now);
        orderDb.AddRange(materialized);
        await orderDb.SaveChangesAsync();

        // Physical truth: group A is already packed, tracked and DISPATCHED; group B is still packed and unshipped.
        var unit = BuildUnit(
            Guid.NewGuid(), checkoutId, sellerA, sellerOrder, $"{tag} A",
            [(lineA, 1m, groupToday), (lineB, 1m, groupTomorrow)], now);
        unit.MarkProcessing(now);
        unit.MarkPacked(now);
        var parcelToday = unit.CreateShipment(
            Guid.NewGuid(), () => Guid.NewGuid(), "پست", [(lineA, 1m)], now,
            "post", "پیشتاز", shippingGroupId: groupToday);
        unit.AssignTracking(parcelToday.ShipmentId, $"{tag}-TRK-A", now);
        unit.ApplyShipmentDispatched(parcelToday.ShipmentId, now);
        Persist(fulfillmentDb, unit);
        await fulfillmentDb.SaveChangesAsync();
        fulfillmentDb.ChangeTracker.Clear();

        var engine = Engine(fulfillmentDb, orderDb, now);
        var page = await engine.QueryAsync(Request(1, 25, tag), CancellationToken.None);

        Assert.Equal(2, page.TotalCount);
        var parcelRow = page.Items.Single(x => x.WorkUnitKind == AdminFulfillmentWorkUnitKinds.Parcel);
        var groupRow = page.Items.Single(x => x.WorkUnitKind == AdminFulfillmentWorkUnitKinds.Group);

        // The dispatched parcel is transit for ITS group — never pending work, never the sibling's target.
        Assert.Equal(AdminFulfillmentWorkUnitKinds.Key(AdminFulfillmentWorkUnitKinds.Parcel, parcelToday.ShipmentId), parcelRow.WorkUnitKey);
        Assert.Equal(parcelToday.ShipmentId, parcelRow.TargetShipmentId);
        Assert.Equal(groupToday, parcelRow.TargetShippingGroupId);
        Assert.Equal(AdminFulfillmentDispatchBucketNames.InTransit, parcelRow.DispatchBucket);
        Assert.False(parcelRow.PendingDispatch);

        // The still-packable sibling must NOT inherit group A's today plan.
        Assert.Equal(AdminFulfillmentWorkUnitKinds.Key(AdminFulfillmentWorkUnitKinds.Group, groupTomorrow), groupRow.WorkUnitKey);
        Assert.Equal(groupTomorrow, groupRow.TargetShippingGroupId);
        Assert.Equal(AdminFulfillmentDispatchBucketNames.Upcoming, groupRow.DispatchBucket);
        Assert.Equal(carrierToday.AddDays(1), groupRow.PlannedHandoffDate);
        Assert.True(groupRow.PendingDispatch);
        Assert.Equal("create_shipment", groupRow.NextStepCode);
        Assert.Equal(1m, groupRow.ScopeRemainingQuantity);

        var counts = await engine.CountAsync(Request(1, 1, tag), CancellationToken.None);
        Assert.Equal(1, counts.TotalActive);
        Assert.Equal(0, counts.Today);
        Assert.Equal(0, counts.Overdue);
        Assert.Equal(1, counts.Upcoming);
        Assert.Equal(0, counts.Unspecified);
        Assert.Equal(1, counts.InTransit);
        Assert.Equal(0, counts.Due);

        await WriteEvidenceAsync(tag, $"caseA rows=2 parcel=InTransit/{groupToday} group=Upcoming/{groupTomorrow} "
            + "counts=active1/today0/overdue0/upcoming1/unspecified0/transit1/due0 targetB=groupB");
    }

    [Fact]
    public async Task Case_A2_a_group_without_plan_never_borrows_a_sibling_groups_plan()
    {
        if (Connection is null)
        {
            return;
        }

        var target = CanonicalTarget();
        var now = DateTimeOffset.UtcNow;
        var carrierToday = AdminFulfillmentDispatchTimingAuthority.LocalDate(now, "Asia/Tehran");
        var tag = $"TB260R1-A2-{DateTime.UtcNow:HHmmss}";

        var checkoutId = Guid.NewGuid();
        var seller = Guid.NewGuid();
        var sellerOrder = Guid.NewGuid();
        var lineNoPlan = Guid.NewGuid();
        var linePlanned = Guid.NewGuid();
        var cartNoPlan = Guid.NewGuid();
        var cartPlanned = Guid.NewGuid();
        var groupNoPlan = Guid.NewGuid();
        var groupPlanned = Guid.NewGuid();

        await using var orderDb = NewOrderDb(target);
        await using var fulfillmentDb = NewFulfillmentDb(target);

        await InsertCheckoutAsync(orderDb, checkoutId, tag, now);
        await InsertSellerOrderAsync(orderDb, sellerOrder, checkoutId, $"{tag}-1", seller);
        await InsertOrderLineAsync(orderDb, lineNoPlan, sellerOrder, seller);
        await InsertOrderLineAsync(orderDb, linePlanned, sellerOrder, seller);

        var planned = ShippingGroupPlannedDispatch.Create(
            carrierToday.AddDays(2),
            new TimeOnly(8, 0).Ticks,
            new TimeOnly(12, 0).Ticks,
            "Asia/Tehran",
            transportWorkingDays: 1,
            policyVersion: 1,
            scheduleSnapshotId: Guid.NewGuid(),
            scheduleSourceRevision: 9);

        orderDb.AddRange(OrderShippingGroupMaterializer.Materialize(
            [
                // Legacy accepted evidence WITHOUT a plan: it must stay explicitly «مشخص نشده».
                Plan(groupNoPlan, seller, cartNoPlan, Evidence(carrierToday, planned: null), carrierToday),
                Plan(groupPlanned, seller, cartPlanned, Evidence(carrierToday.AddDays(2), planned), carrierToday.AddDays(2)),
            ],
            [(lineNoPlan, seller), (linePlanned, seller)],
            new Dictionary<Guid, Guid> { [seller] = sellerOrder },
            new Dictionary<Guid, Guid> { [cartNoPlan] = lineNoPlan, [cartPlanned] = linePlanned },
            checkoutId,
            now));
        await orderDb.SaveChangesAsync();

        var unit = BuildUnit(
            Guid.NewGuid(), checkoutId, seller, sellerOrder, $"{tag} A",
            [(lineNoPlan, 1m, groupNoPlan), (linePlanned, 1m, groupPlanned)], now);
        Persist(fulfillmentDb, unit);
        await fulfillmentDb.SaveChangesAsync();
        fulfillmentDb.ChangeTracker.Clear();

        var page = await Engine(fulfillmentDb, orderDb, now).QueryAsync(Request(1, 25, tag), CancellationToken.None);

        Assert.Equal(2, page.TotalCount);
        var withoutPlan = page.Items.Single(x => x.TargetShippingGroupId == groupNoPlan);
        var withPlan = page.Items.Single(x => x.TargetShippingGroupId == groupPlanned);

        Assert.Equal(AdminFulfillmentDispatchBucketNames.Unspecified, withoutPlan.DispatchBucket);
        Assert.Null(withoutPlan.PlannedHandoffDate);
        Assert.Null(withoutPlan.DispatchDeadlineUtc);
        Assert.Equal(AdminFulfillmentWorkUnitKinds.Key(AdminFulfillmentWorkUnitKinds.Group, groupNoPlan), withoutPlan.WorkUnitKey);

        Assert.Equal(AdminFulfillmentDispatchBucketNames.Upcoming, withPlan.DispatchBucket);
        Assert.Equal(carrierToday.AddDays(2), withPlan.PlannedHandoffDate);
        Assert.Equal("08:00–12:00", withPlan.PlannedHandoffWindow);

        await WriteEvidenceAsync(tag, "caseA2 rows=2 noPlanGroup=Unspecified/null plannedGroup=Upcoming/handoff+2 "
            + "noLending=true");
    }

    // ---------------------------------------------------------------- B

    [Fact]
    public async Task Case_B_two_independent_created_parcels_are_two_stable_rows_paged_once_each()
    {
        if (Connection is null)
        {
            return;
        }

        var target = CanonicalTarget();
        var now = DateTimeOffset.UtcNow;
        var carrierToday = AdminFulfillmentDispatchTimingAuthority.LocalDate(now, "Asia/Tehran");
        var tag = $"TB260R1-B-{DateTime.UtcNow:HHmmss}";

        var checkoutId = Guid.NewGuid();
        var seller = Guid.NewGuid();
        var sellerOrder = Guid.NewGuid();
        var line = Guid.NewGuid();
        var cartLine = Guid.NewGuid();
        var group = Guid.NewGuid();

        await using var orderDb = NewOrderDb(target);
        await using var fulfillmentDb = NewFulfillmentDb(target);

        await InsertCheckoutAsync(orderDb, checkoutId, tag, now);
        await InsertSellerOrderAsync(orderDb, sellerOrder, checkoutId, $"{tag}-1", seller);
        await InsertOrderLineAsync(orderDb, line, sellerOrder, seller);

        var plan = ShippingGroupPlannedDispatch.Create(
            carrierToday.AddDays(1),
            new TimeOnly(9, 0).Ticks,
            new TimeOnly(13, 0).Ticks,
            "Asia/Tehran",
            transportWorkingDays: 1,
            policyVersion: 1,
            scheduleSnapshotId: Guid.NewGuid(),
            scheduleSourceRevision: 3);
        orderDb.AddRange(OrderShippingGroupMaterializer.Materialize(
            [Plan(group, seller, cartLine, Evidence(carrierToday.AddDays(1), plan), carrierToday.AddDays(1))],
            [(line, seller)],
            new Dictionary<Guid, Guid> { [seller] = sellerOrder },
            new Dictionary<Guid, Guid> { [cartLine] = line },
            checkoutId,
            now));
        await orderDb.SaveChangesAsync();

        var unit = BuildUnit(
            Guid.NewGuid(), checkoutId, seller, sellerOrder, $"{tag} B", [(line, 2m, group)], now);
        unit.MarkProcessing(now);
        unit.MarkPacked(now);
        var first = unit.CreateShipment(
            Guid.NewGuid(), () => Guid.NewGuid(), "پست", [(line, 1m)], now, "post", "پیشتاز", shippingGroupId: group);
        var second = unit.CreateShipment(
            Guid.NewGuid(), () => Guid.NewGuid(), "پست", [(line, 1m)], now, "post", "پیشتاز", shippingGroupId: group);
        Persist(fulfillmentDb, unit);
        await fulfillmentDb.SaveChangesAsync();
        fulfillmentDb.ChangeTracker.Clear();

        var engine = Engine(fulfillmentDb, orderDb, now);
        var page = await engine.QueryAsync(Request(1, 25, tag), CancellationToken.None);

        Assert.Equal(2, page.TotalCount);
        Assert.All(page.Items, row => Assert.Equal(AdminFulfillmentWorkUnitKinds.Parcel, row.WorkUnitKind));
        var keys = page.Items.Select(x => x.WorkUnitKey).ToArray();
        Assert.Equal(2, keys.Distinct(StringComparer.Ordinal).Count());
        Assert.Contains(AdminFulfillmentWorkUnitKinds.Key(AdminFulfillmentWorkUnitKinds.Parcel, first.ShipmentId), keys);
        Assert.Contains(AdminFulfillmentWorkUnitKinds.Key(AdminFulfillmentWorkUnitKinds.Parcel, second.ShipmentId), keys);
        Assert.All(page.Items, row =>
        {
            Assert.Equal(group, row.TargetShippingGroupId);
            Assert.Equal(carrierToday.AddDays(1), row.PlannedHandoffDate);
            Assert.Equal(AdminFulfillmentDispatchBucketNames.Upcoming, row.DispatchBucket);
            Assert.Equal(1m, row.ScopeQuantity);
            Assert.Equal(0m, row.ScopeRemainingQuantity);
            Assert.Equal("assign_tracking", row.NextStepCode);
        });
        // No group row: the only packed remainder is already covered by the two open parcels.
        Assert.DoesNotContain(page.Items, row => row.WorkUnitKind == AdminFulfillmentWorkUnitKinds.Group);

        var pageOne = await engine.QueryAsync(Request(1, 1, tag), CancellationToken.None);
        var pageTwo = await engine.QueryAsync(Request(2, 1, tag), CancellationToken.None);
        Assert.Equal(2, pageOne.TotalCount);
        Assert.Single(pageOne.Items);
        Assert.Single(pageTwo.Items);
        Assert.NotEqual(pageOne.Items[0].WorkUnitKey, pageTwo.Items[0].WorkUnitKey);
        Assert.Equal(keys.OrderBy(x => x, StringComparer.Ordinal), pageOne.Items.Concat(pageTwo.Items)
            .Select(x => x.WorkUnitKey).OrderBy(x => x, StringComparer.Ordinal));

        await WriteEvidenceAsync(tag, $"caseB rows=2 distinctKeys=2 locators={first.ShipmentId},{second.ShipmentId} "
            + "bucket=Upcoming plan=shared-own-group pageSize1=2x1 groupRow=0");
    }

    // ---------------------------------------------------------------- C

    [Fact]
    public async Task Case_C_common_package_is_one_parent_row_while_separate_packing_keeps_two_rows()
    {
        if (Connection is null)
        {
            return;
        }

        var target = CanonicalTarget();
        var now = DateTimeOffset.UtcNow;
        var carrierToday = AdminFulfillmentDispatchTimingAuthority.LocalDate(now, "Asia/Tehran");

        // ---- C1: one active common package spanning TWO sellers = ONE parent physical row.
        var packageTag = $"TB260R1-C1-{DateTime.UtcNow:HHmmss}";
        var packageCheckout = Guid.NewGuid();
        await using var orderDb = NewOrderDb(target);
        await using var fulfillmentDb = NewFulfillmentDb(target);

        var sellers = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var sellerOrders = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var lines = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var cartLines = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var groups = new[] { Guid.NewGuid(), Guid.NewGuid() };

        await InsertCheckoutAsync(orderDb, packageCheckout, packageTag, now);
        var planRows = new List<OrderShippingGroupPlan>();
        var units = new List<FulfillmentUnit>();
        var parcels = new List<Shipment>();
        for (var i = 0; i < 2; i++)
        {
            await InsertSellerOrderAsync(orderDb, sellerOrders[i], packageCheckout, $"{packageTag}-{i}", sellers[i]);
            await InsertOrderLineAsync(orderDb, lines[i], sellerOrders[i], sellers[i]);
            var plan = ShippingGroupPlannedDispatch.Create(
                carrierToday.AddDays(1),
                new TimeOnly(10, 0).Ticks,
                new TimeOnly(14, 0).Ticks,
                "Asia/Tehran",
                transportWorkingDays: 1,
                policyVersion: 1,
                scheduleSnapshotId: Guid.NewGuid(),
                scheduleSourceRevision: 5);
            planRows.Add(Plan(groups[i], sellers[i], cartLines[i],
                Evidence(carrierToday.AddDays(1), plan), carrierToday.AddDays(1)));

            var unit = BuildUnit(
                Guid.NewGuid(), packageCheckout, sellers[i], sellerOrders[i], $"{packageTag} {i}",
                [(lines[i], 1m, groups[i])], now);
            unit.MarkProcessing(now);
            unit.MarkPacked(now);
            parcels.Add(unit.CreateShipment(
                Guid.NewGuid(), () => Guid.NewGuid(), "پست", [(lines[i], 1m)], now,
                "post", "پیشتاز", shippingGroupId: groups[i]));
            units.Add(unit);
        }

        orderDb.AddRange(OrderShippingGroupMaterializer.Materialize(
            planRows,
            [(lines[0], sellers[0]), (lines[1], sellers[1])],
            new Dictionary<Guid, Guid> { [sellers[0]] = sellerOrders[0], [sellers[1]] = sellerOrders[1] },
            new Dictionary<Guid, Guid> { [cartLines[0]] = lines[0], [cartLines[1]] = lines[1] },
            packageCheckout,
            now));
        await orderDb.SaveChangesAsync();

        var package = ConsolidatedPackage.Create(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            packageCheckout,
            [(parcels[0].ShipmentId, sellers[0], units[0].FulfillmentId),
             (parcels[1].ShipmentId, sellers[1], units[1].FulfillmentId)],
            "post",
            "پیشتاز",
            trackingReference: null,
            note: null,
            createdBy: null,
            now);

        foreach (var unit in units)
        {
            Persist(fulfillmentDb, unit);
        }
        fulfillmentDb.ConsolidatedPackages.Add(package);
        fulfillmentDb.ConsolidatedPackageMembers.AddRange(package.Members);
        await fulfillmentDb.SaveChangesAsync();
        fulfillmentDb.ChangeTracker.Clear();

        var packageEngine = Engine(fulfillmentDb, orderDb, now);
        var packagePage = await packageEngine.QueryAsync(Request(1, 25, packageTag), CancellationToken.None);

        Assert.Equal(1, packagePage.TotalCount);
        var packageRow = Assert.Single(packagePage.Items);
        Assert.Equal(AdminFulfillmentWorkUnitKinds.Package, packageRow.WorkUnitKind);
        Assert.Equal(AdminFulfillmentWorkUnitKinds.Key(AdminFulfillmentWorkUnitKinds.Package, package.ConsolidatedPackageId), packageRow.WorkUnitKey);
        Assert.Equal(package.ConsolidatedPackageId, packageRow.TargetPackageId);
        Assert.Equal(package.PackageNumber, packageRow.TargetPackageNumber);
        Assert.Null(packageRow.TargetShipmentId);
        Assert.DoesNotContain(packagePage.Items, row => row.WorkUnitKind == AdminFulfillmentWorkUnitKinds.Parcel);
        Assert.Equal("assign_consolidated_package_tracking", packageRow.NextStepCode);
        Assert.Equal(2m, packageRow.ScopeQuantity);

        var packageCounts = await packageEngine.CountAsync(Request(1, 1, packageTag), CancellationToken.None);
        Assert.Equal(1, packageCounts.TotalActive);
        Assert.Equal(1, packageCounts.Upcoming);

        // ---- C2: TB-259 separate packing = independent physical parcels, TWO rows, no artificial package.
        var separateTag = $"TB260R1-C2-{DateTime.UtcNow:HHmmss}";
        var separateCheckout = Guid.NewGuid();
        var separateSellers = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var separateOrders = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var separateLines = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var separateCartLines = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var separateGroups = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var separateRows = new List<OrderShippingGroupPlan>();
        var separateUnits = new List<FulfillmentUnit>();
        var separateParcels = new List<Shipment>();

        await InsertCheckoutAsync(orderDb, separateCheckout, separateTag, now);
        for (var i = 0; i < 2; i++)
        {
            await InsertSellerOrderAsync(orderDb, separateOrders[i], separateCheckout, $"{separateTag}-{i}", separateSellers[i]);
            await InsertOrderLineAsync(orderDb, separateLines[i], separateOrders[i], separateSellers[i]);
            var plan = ShippingGroupPlannedDispatch.Create(
                carrierToday.AddDays(1),
                new TimeOnly(11, 0).Ticks,
                new TimeOnly(15, 0).Ticks,
                "Asia/Tehran",
                transportWorkingDays: 1,
                policyVersion: 1,
                scheduleSnapshotId: Guid.NewGuid(),
                scheduleSourceRevision: 5);
            separateRows.Add(Plan(separateGroups[i], separateSellers[i], separateCartLines[i],
                Evidence(carrierToday.AddDays(1), plan), carrierToday.AddDays(1)));

            var unit = BuildUnit(
                Guid.NewGuid(), separateCheckout, separateSellers[i], separateOrders[i], $"{separateTag} {i}",
                [(separateLines[i], 1m, separateGroups[i])], now);
            unit.MarkProcessing(now);
            unit.MarkPacked(now);
            separateParcels.Add(unit.CreateShipment(
                Guid.NewGuid(), () => Guid.NewGuid(), "پست", [(separateLines[i], 1m)], now,
                "post", "پیشتاز", shippingGroupId: separateGroups[i]));
            separateUnits.Add(unit);
        }

        orderDb.AddRange(OrderShippingGroupMaterializer.Materialize(
            separateRows,
            [(separateLines[0], separateSellers[0]), (separateLines[1], separateSellers[1])],
            new Dictionary<Guid, Guid>
            {
                [separateSellers[0]] = separateOrders[0],
                [separateSellers[1]] = separateOrders[1],
            },
            new Dictionary<Guid, Guid>
            {
                [separateCartLines[0]] = separateLines[0],
                [separateCartLines[1]] = separateLines[1],
            },
            separateCheckout,
            now));
        await orderDb.SaveChangesAsync();
        foreach (var unit in separateUnits)
        {
            Persist(fulfillmentDb, unit);
        }
        await fulfillmentDb.SaveChangesAsync();
        fulfillmentDb.ChangeTracker.Clear();

        var separatePage = await Engine(fulfillmentDb, orderDb, now)
            .QueryAsync(Request(1, 25, separateTag), CancellationToken.None);
        Assert.Equal(2, separatePage.TotalCount);
        Assert.All(separatePage.Items, row =>
        {
            Assert.Equal(AdminFulfillmentWorkUnitKinds.Parcel, row.WorkUnitKind);
            Assert.Null(row.TargetPackageId);
        });
        Assert.Equal(
            separateParcels.Select(x => x.ShipmentId).OrderBy(x => x).ToArray(),
            separatePage.Items.Select(x => x.TargetShipmentId!.Value).OrderBy(x => x).ToArray());

        await WriteEvidenceAsync(packageTag,
            $"caseC packageRows=1 kind=Package locator={package.ConsolidatedPackageId} number={package.PackageNumber} "
            + $"childParcelRows=0 scopeQty=2 counts=active1/upcoming1 separateRows={separatePage.TotalCount}");
    }

    // ---------------------------------------------------------------- D

    [Fact]
    public async Task Case_D_mixed_remainder_and_created_resend_stay_scoped_and_conserved()
    {
        if (Connection is null)
        {
            return;
        }

        var target = CanonicalTarget();
        var now = DateTimeOffset.UtcNow;
        var carrierToday = AdminFulfillmentDispatchTimingAuthority.LocalDate(now, "Asia/Tehran");
        var tag = $"TB260R1-D-{DateTime.UtcNow:HHmmss}";

        var checkoutId = Guid.NewGuid();
        var seller = Guid.NewGuid();
        var sellerOrder = Guid.NewGuid();
        var line = Guid.NewGuid();
        var cartLine = Guid.NewGuid();
        var group = Guid.NewGuid();

        await using var orderDb = NewOrderDb(target);
        await using var fulfillmentDb = NewFulfillmentDb(target);

        await InsertCheckoutAsync(orderDb, checkoutId, tag, now);
        await InsertSellerOrderAsync(orderDb, sellerOrder, checkoutId, $"{tag}-1", seller);
        await InsertOrderLineAsync(orderDb, line, sellerOrder, seller);

        var plan = ShippingGroupPlannedDispatch.Create(
            carrierToday.AddDays(1),
            new TimeOnly(9, 0).Ticks,
            new TimeOnly(13, 0).Ticks,
            "Asia/Tehran",
            transportWorkingDays: 1,
            policyVersion: 1,
            scheduleSnapshotId: Guid.NewGuid(),
            scheduleSourceRevision: 4);
        orderDb.AddRange(OrderShippingGroupMaterializer.Materialize(
            [Plan(group, seller, cartLine, Evidence(carrierToday.AddDays(1), plan), carrierToday.AddDays(1))],
            [(line, seller)],
            new Dictionary<Guid, Guid> { [seller] = sellerOrder },
            new Dictionary<Guid, Guid> { [cartLine] = line },
            checkoutId,
            now));
        await orderDb.SaveChangesAsync();

        var unit = BuildUnit(
            Guid.NewGuid(), checkoutId, seller, sellerOrder, $"{tag} D", [(line, 4m, group)], now);
        unit.MarkProcessing(now);
        unit.MarkPacked(now);

        // Parcel 1 travels the full 258 chain and ends physically received back at origin (usable 1).
        var returned = unit.CreateShipment(
            Guid.NewGuid(), () => Guid.NewGuid(), "پست", [(line, 1m)], now, "post", "پیشتاز", shippingGroupId: group);
        unit.AssignTracking(returned.ShipmentId, $"{tag}-TRK-1", now);
        unit.ApplyShipmentDispatched(returned.ShipmentId, now);
        unit.RecordShipmentNonDelivery(returned.ShipmentId, "customer_unavailable", "TB260R1", Guid.NewGuid(), now);
        unit.StartShipmentReturn(returned.ShipmentId, Guid.NewGuid(), now);
        unit.RecordReturnReceipt(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            returned.ShipmentId,
            Guid.NewGuid(),
            [(line, 1m, ReturnReceiptCondition.Usable)],
            now,
            null,
            now);

        // A Created resend funded by that receipt: it must NOT inherit the group's accepted plan.
        var resend = unit.CreateResendShipment(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            returned.ShipmentId,
            Guid.NewGuid(),
            "پست",
            [(line, 1m)],
            now,
            "post",
            "پیشتاز",
            $"{tag}-resend");
        Assert.NotNull(resend.ResendOfShipmentId);

        // A second, independent Created parcel covering one more packed unit.
        var open = unit.CreateShipment(
            Guid.NewGuid(), () => Guid.NewGuid(), "پست", [(line, 1m)], now, "post", "پیشتاز", shippingGroupId: group);

        Persist(fulfillmentDb, unit);
        fulfillmentDb.ShipmentReturnReceipts.AddRange(unit.ReturnReceipts);
        fulfillmentDb.ShipmentReturnReceiptItems.AddRange(unit.ReturnReceipts.SelectMany(x => x.Items));
        await fulfillmentDb.SaveChangesAsync();
        fulfillmentDb.ChangeTracker.Clear();

        var page = await Engine(fulfillmentDb, orderDb, now).QueryAsync(Request(1, 25, tag), CancellationToken.None);

        var groupRow = page.Items.Single(x => x.WorkUnitKind == AdminFulfillmentWorkUnitKinds.Group);
        // packed 4 − shipped 1 − open Created first allocations 1 = 2. The resend parcel spends returned
        // capacity, never the packed remainder, so no phantom first-allocation quantity appears.
        Assert.Equal(2m, groupRow.ScopeRemainingQuantity);
        Assert.Equal("create_shipment", groupRow.NextStepCode);
        Assert.Equal(AdminFulfillmentDispatchBucketNames.Upcoming, groupRow.DispatchBucket);
        Assert.Equal(group, groupRow.TargetShippingGroupId);

        var resendRow = page.Items.Single(x => x.TargetShipmentId == resend.ShipmentId);
        Assert.Equal(AdminFulfillmentDispatchBucketNames.Unspecified, resendRow.DispatchBucket);
        Assert.Null(resendRow.PlannedHandoffDate);
        Assert.Null(resendRow.DispatchDeadlineUtc);
        Assert.Equal(0m, resendRow.ScopeRemainingQuantity);

        var openRow = page.Items.Single(x => x.TargetShipmentId == open.ShipmentId);
        Assert.Equal(AdminFulfillmentDispatchBucketNames.Upcoming, openRow.DispatchBucket);
        Assert.Equal(0m, openRow.ScopeRemainingQuantity);

        var returnedRow = page.Items.Single(x => x.TargetShipmentId == returned.ShipmentId);
        Assert.Equal(AdminFulfillmentDispatchBucketNames.InTransit, returnedRow.DispatchBucket);
        // Its usable unit is already committed to the live resend, so the truthful next step is none.
        Assert.DoesNotContain("resend_shipment", returnedRow.AvailableActionCodes);
        Assert.NotNull(returnedRow.NextStepBlockerFa);

        Assert.Equal(4, page.TotalCount);
        await WriteEvidenceAsync(tag,
            $"caseD rows=4 groupRemaining=2 openParcelScope=0 resendBucket=Unspecified resendPlan=null "
            + $"returnedResendable=0 returnedBlocker=true total={page.TotalCount}");
    }

    // ---------------------------------------------------------------- helpers

    private static (string Host, int Port, string Database) CanonicalTarget()
    {
        var builder = new NpgsqlConnectionStringBuilder(Connection!);
        Assert.Equal("127.0.0.1", builder.Host);
        Assert.Equal(5432, builder.Port);
        Assert.Equal("tooba_alpha", builder.Database);
        return (builder.Host, builder.Port, builder.Database);
    }

    private static GridQueryRequest Request(int page, int pageSize, string search, string? queueFilter = null) =>
        new(
            page,
            pageSize,
            search,
            [],
            queueFilter is null
                ? []
                : [new GridFilterRequest("queueFilter", "equals", queueFilter, null, null)]);

    private static AdminFulfillmentWorkQueueQueryEngine Engine(
        FulfillmentDbContext fulfillmentDb,
        OrderDbContext orderDb,
        DateTimeOffset now) =>
        new(
            fulfillmentDb,
            new EmptyPartyLookup(),
            new OrderGridEnrichmentBridge(orderDb),
            new OrderDispatchPlanFactsBridge(orderDb),
            new FixedClock(now));

    private static FulfillmentUnit BuildUnit(
        Guid fulfillmentId,
        Guid checkoutId,
        Guid sellerPartyId,
        Guid sellerOrderId,
        string recipient,
        IReadOnlyList<(Guid LineId, decimal Quantity, Guid ShippingGroupId)> lines,
        DateTimeOffset now)
    {
        var unit = FulfillmentUnit.CreateFromPaidOrder(
            fulfillmentId,
            () => Guid.NewGuid(),
            sellerOrderId,
            checkoutId,
            sellerPartyId,
            Guid.NewGuid(),
            recipient,
            "09120000000",
            "تهران",
            "تهران",
            recipient,
            "1234567890",
            "post",
            "پیشتاز",
            lines.Select(x => (x.LineId, x.Quantity, (Guid?)null)).ToList(),
            now,
            lines
                .GroupBy(x => x.ShippingGroupId)
                .Select(group => new FulfillmentShippingGroupPlan(
                    group.Key,
                    sellerPartyId,
                    "post",
                    null,
                    null,
                    "پیشتاز",
                    null,
                    DateOnly.FromDateTime(now.UtcDateTime),
                    null,
                    null,
                    1,
                    group.Select(x => x.LineId).ToList()))
                .ToList());
        return unit;
    }

    private static void Persist(FulfillmentDbContext db, FulfillmentUnit unit)
    {
        db.Fulfillments.Add(unit);
        db.Items.AddRange(unit.Items);
        db.ShippingGroups.AddRange(unit.ShippingGroups);
        db.Shipments.AddRange(unit.Shipments);
        db.ShipmentItems.AddRange(unit.Shipments.SelectMany(x => x.Items));
    }

    private static OrderShippingGroupPlan Plan(
        Guid groupId,
        Guid sellerPartyId,
        Guid cartLineId,
        ShippingGroupAcceptedEvidence evidence,
        DateOnly receivingDate) =>
        new(
            groupId,
            sellerPartyId,
            "post",
            null,
            null,
            "پیشتاز",
            null,
            105_000m,
            "IRR",
            DateOnly.FromDateTime(DateTime.UtcNow),
            // The group's selected delivery date must equal the accepted evidence date; a plan-less (legacy)
            // group is still a real accepted group, only its durable plan is absent.
            receivingDate,
            "09:00-17:00",
            1,
            [cartLineId],
            evidence);

    private static ShippingGroupAcceptedEvidence Evidence(
        DateOnly receivingDate,
        ShippingGroupPlannedDispatch? planned) =>
        ShippingGroupAcceptedEvidence.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            42L,
            ShippingGroupAcceptedEvidence.CarrierScheduleFormatVersion,
            1L,
            ShippingGroupOwner.Store("store-alpha"),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            100_000m,
            5_000m,
            "IRR",
            105_000m,
            0m,
            receivingDate,
            new TimeOnly(9, 0).Ticks,
            new TimeOnly(17, 0).Ticks,
            "Asia/Tehran",
            1,
            3,
            null,
            planned);

    private static OrderDbContext NewOrderDb((string Host, int Port, string Database) target)
    {
        var builder = new NpgsqlConnectionStringBuilder(Connection!);
        var options = new DbContextOptionsBuilder<OrderDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options, builder.ConnectionString, OrderDbContext.Schema, typeof(OrderDbContext));
        return new OrderDbContext(options.Options);
    }

    private static FulfillmentDbContext NewFulfillmentDb((string Host, int Port, string Database) target)
    {
        var builder = new NpgsqlConnectionStringBuilder(Connection!);
        var options = new DbContextOptionsBuilder<FulfillmentDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options, builder.ConnectionString, FulfillmentDbContext.Schema, typeof(FulfillmentDbContext));
        return new FulfillmentDbContext(options.Options);
    }

    private static async Task InsertCheckoutAsync(OrderDbContext db, Guid checkoutId, string tag, DateTimeOffset now) =>
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO "order".checkouts
                 (checkout_id, idempotency_key, cart_id, mode, buyer_party_id, placed_by_user_id, market, currency, channel, created_at, submitted_at)
             VALUES
                 ({checkoutId}, {tag}, {Guid.NewGuid()}, {"OnlinePurchase"}, {Guid.NewGuid()},
                  {Guid.NewGuid()}, {"IR"}, {"IRR"}, {"Marketplace"}, {now}, {now})
             """);

    private static async Task InsertSellerOrderAsync(
        OrderDbContext db, Guid sellerOrderId, Guid checkoutId, string orderNumber, Guid sellerPartyId) =>
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO "order".seller_orders
                 (seller_order_id, checkout_id, order_number, seller_party_id, status, subtotal_snapshot, tax_snapshot, discount_snapshot, grand_total_snapshot, currency)
             VALUES
                 ({sellerOrderId}, {checkoutId}, {orderNumber}, {sellerPartyId}, {"Paid"}, {0m}, {0m}, {0m}, {0m}, {"IRR"})
             """);

    private static async Task InsertOrderLineAsync(
        OrderDbContext db, Guid orderLineId, Guid sellerOrderId, Guid sellerPartyId) =>
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO "order".order_lines
                 (line_id, seller_order_id, offer_id, catalog_variant_id, seller_party_id, quantity, unit_price_snapshot, line_total_snapshot, currency, tax_exclusive, price_id)
             VALUES
                 ({orderLineId}, {sellerOrderId}, {Guid.NewGuid()}, {Guid.NewGuid()}, {sellerPartyId}, {1m}, {100_000m}, {100_000m}, {"IRR"}, {true}, {Guid.NewGuid()})
             """);

    private async Task WriteEvidenceAsync(string tag, string detail)
    {
        _output.WriteLine($"{tag} {detail}");
        var directory = Environment.GetEnvironmentVariable("TB260R1_EVIDENCE_DIR");
        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        Directory.CreateDirectory(directory);
        await File.AppendAllTextAsync(
            Path.Combine(directory, "tb260r1-relational.txt"),
            $"{tag} {detail}{Environment.NewLine}");
    }

    private sealed class EmptyPartyLookup : IPartyLookup
    {
        public Task<PartyLookupResult?> FindByIdAsync(Guid partyId, CancellationToken cancellationToken) =>
            Task.FromResult<PartyLookupResult?>(null);

        public Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(
            IReadOnlyList<Guid> partyIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());

        public Task<IReadOnlyList<Guid>> SearchIdsByDisplayNameAsync(
            string term, int take, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);

        public Task<IReadOnlyList<Guid>> FilterIdsByDisplayNameAsync(
            string? op, string? value, IReadOnlyList<string>? values, int take, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);
    }
}


