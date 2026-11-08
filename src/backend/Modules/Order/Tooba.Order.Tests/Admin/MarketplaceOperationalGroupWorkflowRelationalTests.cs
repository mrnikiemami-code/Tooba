using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application.Fulfillments.Models;
using Tooba.Fulfillment.Application.Fulfillments.Ports;
using Tooba.Fulfillment.Contracts.Operations;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Fulfillment.Infrastructure.Adapters;
using Tooba.Fulfillment.Infrastructure.Directories;
using Tooba.Fulfillment.Infrastructure.Observability;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Policies;
using Tooba.Order.Application.Admin.Operations.Ports;
using Tooba.Order.Application.Admin.Operations.Services;
using Tooba.Order.Application.Storefront.Ports;
using Tooba.Order.Contracts.Admin.Operations;
using Tooba.Order.Contracts.Fulfillment;
using Tooba.Order.Contracts.Shipping;
using Tooba.Order.Domain;
using Tooba.Order.Domain.Aggregates;
using Tooba.Order.Domain.Checkout;
using Tooba.Order.Domain.Enums;
using Tooba.Order.Infrastructure.Admin.Operations;
using Tooba.Order.Infrastructure.Integrations.Fulfillment;
using Tooba.Order.Infrastructure.Integrations.Shipping;
using Tooba.Order.Infrastructure.Integrations.Storefront;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Order.Tests.Checkout;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Order.Tests.Admin;

/// <summary>
/// TB-253-R3: the REAL Order path of the Marketplace operational group workflow on the task-owned
/// disposable PostgreSQL — committed Order shipping-group evidence built through the real domain
/// boundaries, the real facts/history readers, the real mapper/authority, the real Fulfillment
/// directory through the production admin adapter, and the public workflow the admin
/// command/orchestrator delegates to.
/// </summary>
/// <remarks>
/// <para>
/// Nothing here hand-crafts a <c>GroupFact</c> or invents a group/line identity: every member, line,
/// owner, origin context, receiving slot and fee comes from persisted Order rows and is read back
/// through <see cref="OrderCommittedCarrierShipmentFactsReader"/> and
/// <see cref="OrderShippingGroupHistoryReader"/>.
/// </para>
/// <para>
/// The access reader is the one controlled seam because platform permission grants live in Host
/// AccessControl, not in Order; it is exercised both ways so the workflow's own authority check is
/// proven, not bypassed.
/// </para>
/// </remarks>
[Collection(OrderEvidenceRelationalCollection.Name)]
public sealed class MarketplaceOperationalGroupWorkflowRelationalTests
{
    private const string EnvVar = "TOOBA_TB253R3_ORDER_POSTGRES";
    private const string ExpectedDatabase = "order_tb253r3";
    private const string ExpectedUser = "tb253r3";

    private static readonly Guid CheckoutId = Guid.Parse("01a1c253-0000-7000-8000-0000000000c1");
    private static readonly Guid CartId = Guid.Parse("01a1c253-0000-7000-8000-0000000000a1");
    private static readonly Guid Buyer = Guid.Parse("01a1c253-0000-7000-8000-0000000000b1");
    private static readonly Guid Actor = Guid.Parse("01a1c253-0000-7000-8000-0000000000d1");
    private static readonly Guid SellerA = Guid.Parse("01a1c253-0000-7000-8000-0000000000e1");
    private static readonly Guid SellerB = Guid.Parse("01a1c253-0000-7000-8000-0000000000e2");
    private static readonly Guid OrderA = Guid.Parse("01a1c253-0000-7000-8000-0000000000f1");
    private static readonly Guid OrderB = Guid.Parse("01a1c253-0000-7000-8000-0000000000f2");
    private static readonly Guid LineA = Guid.Parse("01a1c253-0000-7000-8000-000000000101");
    private static readonly Guid LineB = Guid.Parse("01a1c253-0000-7000-8000-000000000102");
    private static readonly Guid GroupA = Guid.Parse("01a1c253-0000-7000-8000-000000000201");
    private static readonly Guid GroupB = Guid.Parse("01a1c253-0000-7000-8000-000000000202");
    private static readonly Guid SnapshotId = Guid.Parse("01a1c253-0000-7000-8000-000000000301");
    private static readonly Guid DraftId = Guid.Parse("01a1c253-0000-7000-8000-000000000302");
    private static readonly Guid AreaGroup = Guid.Parse("01a1c253-0000-7000-8000-000000000401");
    private static readonly Guid RowA = Guid.Parse("01a1c253-0000-7000-8000-000000000501");
    private static readonly Guid RowB = Guid.Parse("01a1c253-0000-7000-8000-000000000502");
    private static readonly Guid ShippingTypeId = Guid.Parse("01a1c253-0000-7000-8000-000000000601");
    private static readonly string Origin = "WH-1 / Tehran";
    private static readonly DateOnly Day = new(2026, 11, 10);
    private static readonly long OpenTicks = 14L * TimeSpan.TicksPerHour;
    private static readonly long CloseTicks = 15L * TimeSpan.TicksPerHour;
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 9, 0, 0, TimeSpan.Zero);

    private static Guid Occurrence(string suffix) =>
        Guid.Parse($"01a1c253-0000-7000-8000-0000000009{suffix}");

    // ---------------------------------------------------------------- A. origin authority

    [Fact]
    [Trait("Category", "TB253R3")]
    [Trait("Case", "A-origin")]
    public async Task A_missing_origin_keeps_groups_independent_and_is_a_typed_refusal()
    {
        var target = Target();
        await using var orderDb = await ReadyAsync(target);
        await using var fulfillmentDb = await ReadyFulfillmentAsync(target);
        var seeded = await SeedCommittedOrderAsync(orderDb);

        var fixture = await CreateFixtureAsync(orderDb, fulfillmentDb, allowManage: true, seeded, originOverride: _ => null);
        var projected = MarketplaceOperationalDeliveryGroupAuthority.Project(
            fixture.Facts.Select(f => AdminOrderOperationalFacts.ToFact(
                CheckoutId,
                f,
                fixture.History[f.ShippingGroupId])).ToArray());
        Assert.Equal(2, projected.Count);
        Assert.All(projected, g => Assert.Equal(
            MarketplaceOperationalDeliveryGroupAuthority.SeparateMissingOrigin,
            g.SeparateReasonCode));
        Assert.All(projected, g => Assert.False(g.IsCrossSellerMarketplace));

        var error = await Assert.ThrowsAsync<AdminOrderOperationsException>(() =>
            fixture.Workflow.PrepareAsync(
                CheckoutId,
                ReviewRequest(Occurrence("01"), (seeded.LineA, 1m), (seeded.LineB, 1m)),
                Actor,
                CancellationToken.None));
        Assert.Equal(MarketplaceOperationalDeliveryGroupAuthority.WorkflowGroupForeign, error.Code);
        Assert.Equal(0, await fulfillmentDb.Shipments.CountAsync());
        Assert.Equal(0, await fulfillmentDb.ConsolidatedPackages.CountAsync());
    }

    [Fact]
    [Trait("Category", "TB253R3")]
    [Trait("Case", "A-origin")]
    public async Task A_different_physical_origins_stay_separate_even_with_the_same_responsibility()
    {
        var target = Target();
        await using var orderDb = await ReadyAsync(target);
        await using var fulfillmentDb = await ReadyFulfillmentAsync(target);
        var seeded = await SeedCommittedOrderAsync(orderDb);

        var fixture = await CreateFixtureAsync(
            orderDb,
            fulfillmentDb,
            allowManage: true,
            seeded,
            originOverride: g => g == GroupA ? "WH-1 / Tehran" : "WH-2 / Tehran");
        var projected = MarketplaceOperationalDeliveryGroupAuthority.Project(
            fixture.Facts.Select(f => AdminOrderOperationalFacts.ToFact(
                CheckoutId,
                f,
                fixture.History[f.ShippingGroupId])).ToArray());
        Assert.Equal(2, projected.Count);

        var error = await Assert.ThrowsAsync<AdminOrderOperationsException>(() =>
            fixture.Workflow.PrepareAsync(
                CheckoutId,
                ReviewRequest(Occurrence("02"), (seeded.LineA, 1m), (seeded.LineB, 1m)),
                Actor,
                CancellationToken.None));
        Assert.Equal(MarketplaceOperationalDeliveryGroupAuthority.WorkflowGroupForeign, error.Code);
        Assert.Equal(0, await fulfillmentDb.Shipments.CountAsync());
    }

    // ---------------------------------------------------------------- B. occurrence replay/concurrency

    [Fact]
    [Trait("Category", "TB253R3")]
    [Trait("Case", "B-replay")]
    public async Task B_same_occurrence_retry_and_barrier_coordinated_two_context_twin_allocate_once()
    {
        var target = Target();
        await using var orderDb = await ReadyAsync(target);
        await using var fulfillmentDb = await ReadyFulfillmentAsync(target);
        var seeded = await SeedCommittedOrderAsync(orderDb);

        var fixture = await CreateFixtureAsync(orderDb, fulfillmentDb, allowManage: true, seeded, originOverride: _ => Origin);
        var occurrence = Occurrence("11");
        var request = ReviewRequest(occurrence, (seeded.LineA, 2m), (seeded.LineB, 3m));

        var first = Json(await fixture.Workflow.PrepareAsync(CheckoutId, request, Actor, CancellationToken.None));
        var packageId = first.GetProperty("consolidatedPackageId").GetGuid();
        var shipmentIds = first.GetProperty("shipmentIds")
            .EnumerateArray().Select(x => x.GetGuid()).OrderBy(x => x).ToArray();
        Assert.False(first.GetProperty("replayed").GetBoolean());

        var retry = Json(await fixture.Workflow.PrepareAsync(CheckoutId, request, Actor, CancellationToken.None));
        Assert.True(retry.GetProperty("replayed").GetBoolean());
        Assert.Equal(packageId, retry.GetProperty("consolidatedPackageId").GetGuid());
        Assert.Equal(
            shipmentIds,
            retry.GetProperty("shipmentIds").EnumerateArray().Select(x => x.GetGuid()).OrderBy(x => x).ToArray());

        Assert.Equal(2, await fulfillmentDb.Shipments.CountAsync());
        Assert.Equal(1, await fulfillmentDb.ConsolidatedPackages.CountAsync());
    }

    [Fact]
    [Trait("Category", "TB253R3")]
    [Trait("Case", "B-concurrency")]
    public async Task B_two_contexts_started_on_one_barrier_produce_one_allocation_set_and_typed_results()
    {
        var target = Target();
        await using var orderDb = await ReadyAsync(target);
        await using var fulfillmentDb = await ReadyFulfillmentAsync(target);
        var seeded = await SeedCommittedOrderAsync(orderDb);

        var first = await CreateFixtureAsync(orderDb, fulfillmentDb, allowManage: true, seeded, originOverride: _ => Origin);
        // Two independent workflow/db contexts — a real concurrency barrier, not sequential calls.
        await using var orderDb2 = NewOrderContext(target);
        await using var fulfillmentDb2 = NewFulfillmentContext(target);
        var second = await CreateFixtureAsync(orderDb2, fulfillmentDb2, allowManage: true, seeded, originOverride: _ => Origin);

        var occurrence = Occurrence("12");
        var request = ReviewRequest(occurrence, (seeded.LineA, 2m), (seeded.LineB, 3m));

        using var barrier = new Barrier(2);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<object> Run(
            MarketplaceOperationalGroupWorkflow workflow,
            CancellationToken cancellationToken)
        {
            start.Task.GetAwaiter().GetResult();
            barrier.SignalAndWait(TimeSpan.FromSeconds(10));
            return await workflow.PrepareAsync(CheckoutId, request, Actor, cancellationToken);
        }

        var taskA = Task.Run(() => Run(first.Workflow, CancellationToken.None));
        var taskB = Task.Run(() => Run(second.Workflow, CancellationToken.None));
        start.SetResult();
        var results = await Task.WhenAll(taskA, taskB);

        var roots = results.Select(Json).ToArray();
        var packageIds = roots.Select(r => r.GetProperty("consolidatedPackageId").GetGuid()).Distinct().ToArray();
        Assert.Single(packageIds);
        var sets = roots
            .Select(r => r.GetProperty("shipmentIds").EnumerateArray().Select(x => x.GetGuid()).OrderBy(x => x).ToArray())
            .ToArray();
        Assert.Equal(sets[0], sets[1]);
        Assert.Equal(2, sets[0].Length);

        fulfillmentDb.ChangeTracker.Clear();
        Assert.Equal(2, await fulfillmentDb.Shipments.CountAsync());
        Assert.Equal(1, await fulfillmentDb.ConsolidatedPackages.CountAsync());
        Assert.Equal(2, await fulfillmentDb.ConsolidatedPackageMembers.CountAsync());
    }

    // ---------------------------------------------------------------- C. identical next occurrence

    [Fact]
    [Trait("Category", "TB253R3")]
    [Trait("Case", "C-next-occurrence")]
    public async Task C_identical_second_occurrence_allocates_a_new_parcel_set_and_keeps_the_first_package()
    {
        var target = Target();
        await using var orderDb = await ReadyAsync(target);
        await using var fulfillmentDb = await ReadyFulfillmentAsync(target);
        var seeded = await SeedCommittedOrderAsync(orderDb, packedA: 2m, packedB: 2m);

        var fixture = await CreateFixtureAsync(orderDb, fulfillmentDb, allowManage: true, seeded, originOverride: _ => Origin);
        var firstOccurrence = Occurrence("21");
        var nextOccurrence = Occurrence("22");

        var first = Json(await fixture.Workflow.PrepareAsync(
            CheckoutId,
            ReviewRequest(firstOccurrence, (seeded.LineA, 1m), (seeded.LineB, 1m)),
            Actor,
            CancellationToken.None));
        var firstPackage = first.GetProperty("consolidatedPackageId").GetGuid();
        var firstShipments = first.GetProperty("shipmentIds")
            .EnumerateArray().Select(x => x.GetGuid()).OrderBy(x => x).ToArray();

        // Remaining packed quantity after the first partial package is exactly 1 per line.
        var next = Json(await fixture.Workflow.PrepareAsync(
            CheckoutId,
            ReviewRequest(nextOccurrence, (seeded.LineA, 1m), (seeded.LineB, 1m)),
            Actor,
            CancellationToken.None));
        var nextPackage = next.GetProperty("consolidatedPackageId").GetGuid();
        var nextShipments = next.GetProperty("shipmentIds")
            .EnumerateArray().Select(x => x.GetGuid()).OrderBy(x => x).ToArray();

        Assert.NotEqual(firstPackage, nextPackage);
        Assert.Empty(firstShipments.Intersect(nextShipments));
        Assert.Equal(4, await fulfillmentDb.Shipments.CountAsync());
        Assert.Equal(2, await fulfillmentDb.ConsolidatedPackages.CountAsync());

        var items = await fulfillmentDb.ShipmentItems.AsNoTracking().ToListAsync();
        Assert.Equal(2m, items.Where(x => x.OrderLineId == seeded.LineA).Sum(x => x.Quantity));
        Assert.Equal(2m, items.Where(x => x.OrderLineId == seeded.LineB).Sum(x => x.Quantity));

        // Replaying the ORIGINAL occurrence still returns its ORIGINAL package and parcels.
        var replay = Json(await fixture.Workflow.PrepareAsync(
            CheckoutId,
            ReviewRequest(firstOccurrence, (seeded.LineA, 1m), (seeded.LineB, 1m)),
            Actor,
            CancellationToken.None));
        Assert.True(replay.GetProperty("replayed").GetBoolean());
        Assert.Equal(firstPackage, replay.GetProperty("consolidatedPackageId").GetGuid());
        Assert.Equal(
            firstShipments,
            replay.GetProperty("shipmentIds").EnumerateArray().Select(x => x.GetGuid()).OrderBy(x => x).ToArray());
        Assert.Equal(4, await fulfillmentDb.Shipments.CountAsync());
    }

    // ---------------------------------------------------------------- D. preflight and conflicts

    [Fact]
    [Trait("Category", "TB253R3")]
    [Trait("Case", "D-preflight")]
    public async Task D_same_occurrence_mismatch_and_foreign_ids_are_typed_refusals_with_zero_partial_writes()
    {
        var target = Target();
        await using var orderDb = await ReadyAsync(target);
        await using var fulfillmentDb = await ReadyFulfillmentAsync(target);
        var seeded = await SeedCommittedOrderAsync(orderDb, packedA: 4m, packedB: 4m);

        var fixture = await CreateFixtureAsync(orderDb, fulfillmentDb, allowManage: true, seeded, originOverride: _ => Origin);
        var occurrence = Occurrence("31");
        await fixture.Workflow.PrepareAsync(
            CheckoutId,
            ReviewRequest(occurrence, (seeded.LineA, 2m), (seeded.LineB, 2m)),
            Actor,
            CancellationToken.None);
        var recorded = await fulfillmentDb.Shipments.CountAsync();
        Assert.Equal(2, recorded);

        // Same occurrence, changed selection: the recorded allocation is immutable → typed conflict.
        var conflict = await Assert.ThrowsAsync<AdminOrderOperationsException>(() =>
            fixture.Workflow.PrepareAsync(
                CheckoutId,
                ReviewRequest(occurrence, (seeded.LineA, 1m), (seeded.LineB, 1m)),
                Actor,
                CancellationToken.None));
        Assert.Equal(MarketplaceOperationalDeliveryGroupAuthority.WorkflowIntentConflict, conflict.Code);
        Assert.Equal(recorded, await fulfillmentDb.Shipments.CountAsync());

        // Foreign line id: silently dropping it would allocate the rest; it must refuse first.
        var foreignLine = await Assert.ThrowsAsync<AdminOrderOperationsException>(() =>
            fixture.Workflow.PrepareAsync(
                CheckoutId,
                ReviewRequest(Occurrence("32"), (seeded.LineA, 1m), (Guid.NewGuid(), 1m)),
                Actor,
                CancellationToken.None));
        Assert.Equal(MarketplaceOperationalDeliveryGroupAuthority.WorkflowSelectionInvalid, foreignLine.Code);
        Assert.Equal(recorded, await fulfillmentDb.Shipments.CountAsync());

        // Invalid LATER member (seller B over remaining): member A must not be allocated first.
        var later = await Assert.ThrowsAsync<AdminOrderOperationsException>(() =>
            fixture.Workflow.PrepareAsync(
                CheckoutId,
                ReviewRequest(Occurrence("33"), (seeded.LineA, 1m), (seeded.LineB, 99m)),
                Actor,
                CancellationToken.None));
        Assert.Equal(MarketplaceOperationalDeliveryGroupAuthority.WorkflowSelectionInvalid, later.Code);
        Assert.Equal(recorded, await fulfillmentDb.Shipments.CountAsync());

        // Foreign reuse id: every supplied id must be accounted for.
        var foreignReuse = await Assert.ThrowsAsync<AdminOrderOperationsException>(() =>
            fixture.Workflow.PrepareAsync(
                CheckoutId,
                ReviewRequest(Occurrence("34"), (seeded.LineA, 1m), (seeded.LineB, 1m)) with
                {
                    ReuseShipmentIds = [Guid.NewGuid()],
                },
                Actor,
                CancellationToken.None));
        Assert.Equal(MarketplaceOperationalDeliveryGroupAuthority.WorkflowShipmentForeign, foreignReuse.Code);
        Assert.Equal(recorded, await fulfillmentDb.Shipments.CountAsync());

        // Missing occurrence: refused before anything else.
        var noOccurrence = await Assert.ThrowsAsync<AdminOrderOperationsException>(() =>
            fixture.Workflow.PrepareAsync(
                CheckoutId,
                ReviewRequest(Guid.Empty, (seeded.LineA, 1m), (seeded.LineB, 1m)),
                Actor,
                CancellationToken.None));
        Assert.Equal(MarketplaceOperationalDeliveryGroupAuthority.WorkflowOccurrenceRequired, noOccurrence.Code);
        Assert.Equal(recorded, await fulfillmentDb.Shipments.CountAsync());
    }

    // ---------------------------------------------------------------- E. crash between members and package

    [Fact]
    [Trait("Category", "TB253R3")]
    [Trait("Case", "E-recovery")]
    public async Task E_transport_failure_after_constituent_parcels_recovers_exact_ids_on_same_occurrence_retry()
    {
        var target = Target();
        await using var orderDb = await ReadyAsync(target);
        await using var fulfillmentDb = await ReadyFulfillmentAsync(target);
        var seeded = await SeedCommittedOrderAsync(orderDb);

        var occurrence = Occurrence("41");
        var request = ReviewRequest(occurrence, (seeded.LineA, 2m), (seeded.LineB, 3m));

        // Failure AFTER both constituent parcels are committed, BEFORE the common package is persisted:
        // the real persistence boundary fails once, so this is the "response lost / 5xx" window rather
        // than a mocked workflow outcome. The caller only sees a transport exception.
        var interceptor = new FailFirstPackageSaveInterceptor();
        await using (var failingDb = NewFulfillmentContext(target, interceptor))
        {
            var failing = await CreateFixtureAsync(orderDb, failingDb, allowManage: true, seeded, originOverride: _ => Origin);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                failing.Workflow.PrepareAsync(CheckoutId, request, Actor, CancellationToken.None));
            Assert.Equal(0, interceptor.Remaining);
        }

        fulfillmentDb.ChangeTracker.Clear();
        Assert.Equal(2, await fulfillmentDb.Shipments.CountAsync());
        Assert.Equal(0, await fulfillmentDb.ConsolidatedPackages.CountAsync());
        var recordedIds = await fulfillmentDb.Shipments.AsNoTracking()
            .Select(x => x.ShipmentId)
            .OrderBy(x => x)
            .ToListAsync();
        Assert.Equal(2, recordedIds.Count);
        Assert.All(
            await fulfillmentDb.Shipments.AsNoTracking().ToListAsync(),
            s => Assert.StartsWith("opgroup:", s.ShippingDispatchKey!, StringComparison.Ordinal));

        // Retry the SAME occurrence through the real adapter: recorded parcels are reused, no extras.
        var fixture = await CreateFixtureAsync(orderDb, fulfillmentDb, allowManage: true, seeded, originOverride: _ => Origin);
        var recovered = Json(await fixture.Workflow.PrepareAsync(
            CheckoutId, request, Actor, CancellationToken.None));
        fulfillmentDb.ChangeTracker.Clear();
        Assert.Equal(2, await fulfillmentDb.Shipments.CountAsync());
        Assert.Equal(1, await fulfillmentDb.ConsolidatedPackages.CountAsync());
        Assert.Equal(
            recordedIds,
            recovered.GetProperty("shipmentIds").EnumerateArray().Select(x => x.GetGuid()).OrderBy(x => x).ToArray());
        Assert.All(
            recovered.GetProperty("members").EnumerateArray(),
            m => Assert.True(m.GetProperty("replayed").GetBoolean()));

        // Accepted money/evidence and the financial seller orders stay immutable.
        orderDb.ChangeTracker.Clear();
        var orders = await orderDb.SellerOrders.AsNoTracking().Include(x => x.Lines).ToListAsync();
        Assert.Equal(2, orders.Count);
        Assert.All(orders, o => Assert.Equal(SellerOrderStatus.Paid, o.Status));
        Assert.Equal(100_000m, orders.Single(x => x.SellerOrderId == OrderA).Lines.Single().LineTotalSnapshot);
        Assert.Equal(120_000m, orders.Single(x => x.SellerOrderId == OrderB).Lines.Single().LineTotalSnapshot);
        var groups = await orderDb.OrderShippingGroups.AsNoTracking().Include(x => x.AcceptedEvidence).ToListAsync();
        Assert.Equal(2, groups.Count);
        Assert.All(groups, g => Assert.Equal(125_000m, g.ShippingChargeSnapshot));
        Assert.All(groups, g => Assert.Equal(SnapshotId, g.AcceptedEvidence!.SnapshotId));
        Assert.All(groups, g => Assert.Equal(ShippingGroupOwnerKind.Marketplace, g.AcceptedEvidence!.Owner.Kind));
        // The committed row cannot carry origin context (the Order domain refuses unverified origin);
        // the positive grouping above used the controlled origin seam, disclosed in RESULT.
        Assert.All(groups, g => Assert.Null(g.OriginContext));
    }

    // ---------------------------------------------------------------- controls

    [Fact]
    [Trait("Category", "TB253R3")]
    [Trait("Case", "refusals")]
    public async Task Unauthorized_actor_foreign_group_conflicting_method_and_missing_selection_are_typed_refusals()
    {
        var target = Target();
        await using var orderDb = await ReadyAsync(target);
        await using var fulfillmentDb = await ReadyFulfillmentAsync(target);
        var seeded = await SeedCommittedOrderAsync(orderDb);

        var denied = await CreateFixtureAsync(orderDb, fulfillmentDb, allowManage: false, seeded, originOverride: _ => Origin);
        var deniedError = await Assert.ThrowsAsync<AdminOrderOperationsException>(() =>
            denied.Workflow.PrepareAsync(
                CheckoutId,
                ReviewRequest(Occurrence("51"), (seeded.LineA, 2m), (seeded.LineB, 3m)),
                Actor,
                CancellationToken.None));
        Assert.Equal("order.operation.denied", deniedError.Code);
        Assert.Equal(0, await fulfillmentDb.Shipments.CountAsync());
        Assert.Equal(0, await fulfillmentDb.ConsolidatedPackages.CountAsync());

        var fixture = await CreateFixtureAsync(orderDb, fulfillmentDb, allowManage: true, seeded, originOverride: _ => Origin);

        var foreign = await Assert.ThrowsAsync<AdminOrderOperationsException>(() =>
            fixture.Workflow.PrepareAsync(
                CheckoutId,
                ReviewRequestWithGroups(
                    (GroupA, Guid.NewGuid()),
                    Occurrence("52"),
                    (seeded.LineA, 2m),
                    (seeded.LineB, 3m)),
                Actor,
                CancellationToken.None));
        Assert.Equal(MarketplaceOperationalDeliveryGroupAuthority.WorkflowGroupForeign, foreign.Code);

        var method = await Assert.ThrowsAsync<AdminOrderOperationsException>(() =>
            fixture.Workflow.PrepareAsync(
                CheckoutId,
                ReviewRequest(Occurrence("53"), (seeded.LineA, 2m), (seeded.LineB, 3m)) with
                {
                    ShippingMethodCode = "tipax",
                },
                Actor,
                CancellationToken.None));
        Assert.Equal(MarketplaceOperationalDeliveryGroupAuthority.ConsolidateIncompatible, method.Code);

        var empty = await Assert.ThrowsAsync<AdminOrderOperationsException>(() =>
            fixture.Workflow.PrepareAsync(
                CheckoutId,
                ReviewRequest(Occurrence("54")),
                Actor,
                CancellationToken.None));
        Assert.Equal(MarketplaceOperationalDeliveryGroupAuthority.WorkflowSelectionRequired, empty.Code);

        Assert.Equal(0, await fulfillmentDb.Shipments.CountAsync());
        Assert.Equal(0, await fulfillmentDb.ConsolidatedPackages.CountAsync());
    }

    [Fact]
    [Trait("Category", "TB253R3")]
    [Trait("Case", "incompatible-slot")]
    public async Task Committed_groups_with_a_different_receiving_hour_are_not_one_operational_group()
    {
        var target = Target();
        await using var orderDb = await ReadyAsync(target);
        await using var fulfillmentDb = await ReadyFulfillmentAsync(target);
        var seeded = await SeedCommittedOrderAsync(orderDb, shiftGroupBSlot: true);

        var fixture = await CreateFixtureAsync(orderDb, fulfillmentDb, allowManage: true, seeded, originOverride: _ => Origin);

        var error = await Assert.ThrowsAsync<AdminOrderOperationsException>(() =>
            fixture.Workflow.PrepareAsync(
                CheckoutId,
                ReviewRequest(Occurrence("61"), (seeded.LineA, 2m), (seeded.LineB, 3m)),
                Actor,
                CancellationToken.None));

        Assert.Equal(MarketplaceOperationalDeliveryGroupAuthority.WorkflowGroupForeign, error.Code);
        Assert.Equal(0, await fulfillmentDb.Shipments.CountAsync());
    }

    private static OrderEvidenceRelationalTestTarget Target() =>
        OrderEvidenceRelationalTestTarget.FromEnvironment(EnvVar, ExpectedDatabase, ExpectedUser);

    private static JsonElement Json(object value) =>
        JsonDocument.Parse(JsonSerializer.Serialize(value)).RootElement;

    private static AdminOrderOperationRequest ReviewRequest(
        Guid occurrence,
        params (Guid LineId, decimal Quantity)[] lines) =>
        ReviewRequestWithGroups((GroupA, GroupB), occurrence, lines);

    private static AdminOrderOperationRequest ReviewRequestWithGroups(
        (Guid GroupA, Guid GroupB) groups,
        Guid occurrence,
        params (Guid LineId, decimal Quantity)[] lines) =>
        new(
            "prepare_operational_group",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            lines.Select(x => new AdminOrderLineSelection(x.LineId, x.Quantity)).ToArray(),
            null,
            null,
            null,
            null,
            null,
            OperationalGroupShippingGroupIds: [groups.GroupA, groups.GroupB],
            ReuseShipmentIds: null,
            OperationalGroupOccurrenceId: occurrence);

    private static async Task<Fixture> CreateFixtureAsync(
        OrderDbContext orderDb,
        FulfillmentDbContext fulfillmentDb,
        bool allowManage,
        SeededLines seeded,
        Func<Guid, string?>? originOverride = null)
    {
        await EnsurePackedFulfillmentsAsync(orderDb, fulfillmentDb, seeded);
        var adapter = CreateAdapter(fulfillmentDb);
        var factsReader = new OrderCommittedCarrierShipmentFactsReader(orderDb, adapter);
        IOrderShippingGroupHistoryReader historyReader = new OrderShippingGroupHistoryReader(orderDb);
        if (originOverride is not null)
        {
            historyReader = new OriginSeedingHistoryReader(historyReader, originOverride);
        }

        var workflow = new MarketplaceOperationalGroupWorkflow(
            adapter,
            factsReader,
            historyReader,
            new ControlledAccessReader(allowManage),
            new OrderOperationalGroupOccurrenceStore(orderDb, new SystemUtcClock()));
        var facts = await factsReader.ListByCheckoutAsync(CheckoutId, CancellationToken.None);
        var history = await historyReader.GetForCheckoutAsync(CheckoutId, CancellationToken.None);
        return new Fixture(
            workflow,
            facts,
            history.ToDictionary(x => x.ShippingGroupId));
    }

    private static FulfillmentAdminOperationsAdapter CreateAdapter(FulfillmentDbContext fulfillmentDb)
    {
        var directory = CreateFulfillmentDirectory(fulfillmentDb);
        return new FulfillmentAdminOperationsAdapter(
            directory,
            new FulfillmentShippedQuantityReader(fulfillmentDb),
            new ShippingMethodsOptions());
    }

    private static async Task EnsurePackedFulfillmentsAsync(
        OrderDbContext orderDb,
        FulfillmentDbContext fulfillmentDb,
        SeededLines seeded)
    {
        if (await fulfillmentDb.Fulfillments.AnyAsync())
        {
            return;
        }

        var directory = new FulfillmentDirectory(
            fulfillmentDb,
            new OpenFulfillmentUseCaseGuard(),
            new OrderFulfillmentBridge(orderDb),
            new NoopInventoryGateway(),
            new FulfillmentInstrumentation(),
            new SystemUtcClock(),
            new UuidV7IdGenerator());
        // Fresh payment/event identity: a reused event id would be treated as an already-processed payment
        // and the domain handoff would be skipped entirely.
        await directory.CreateFromPaidSellerOrdersAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [OrderA, OrderB],
            CancellationToken.None);
        var packed = new[]
        {
            (OrderId: OrderA, LineId: seeded.LineA, Quantity: seeded.PackedA),
            (OrderId: OrderB, LineId: seeded.LineB, Quantity: seeded.PackedB),
        };
        foreach (var row in packed)
        {
            var unit = await directory.GetBySellerOrderAsync(row.OrderId, CancellationToken.None);
            Assert.NotNull(unit);
            await directory.MarkProcessingAsync(unit!.FulfillmentId, Actor, CancellationToken.None);
            await directory.PackSelectionsAsync(
                unit.FulfillmentId,
                Actor,
                [new Tooba.Fulfillment.Application.Fulfillments.Models.FulfillmentSelectionCommand(row.LineId, row.Quantity)],
                CancellationToken.None);
        }

        fulfillmentDb.ChangeTracker.Clear();
    }

    private static FulfillmentDirectory CreateFulfillmentDirectory(FulfillmentDbContext db) =>
        new(
            db,
            new OpenFulfillmentUseCaseGuard(),
            new NoopOrderReader(),
            new NoopInventoryGateway(),
            new FulfillmentInstrumentation(),
            new SystemUtcClock(),
            new UuidV7IdGenerator());

    private static async Task<SeededLines> SeedCommittedOrderAsync(
        OrderDbContext db,
        bool shiftGroupBSlot = false,
        decimal packedA = 2m,
        decimal packedB = 3m)
    {
        var lineA = OrderLine.FromCheckout(
            OrderA, LineA, Guid.NewGuid(), SellerA, packedA, 50_000m, "IRR", true, Guid.NewGuid(), Guid.NewGuid(),
            "Taxable", 0.09m, 9_000m, 118_000m, null);
        var lineB = OrderLine.FromCheckout(
            OrderB, LineB, Guid.NewGuid(), SellerB, packedB, 40_000m, "IRR", true, Guid.NewGuid(), Guid.NewGuid(),
            "Taxable", 0.09m, 10_800m, 130_800m, null);
        var orderA = SellerOrder.Open(
            CheckoutId, SellerA, $"TB253R3A{OrderA:N}"[..20], OrderMode.OnlinePurchase, "IRR", [lineA]);
        var orderB = SellerOrder.Open(
            CheckoutId, SellerB, $"TB253R3B{OrderB:N}"[..20], OrderMode.OnlinePurchase, "IRR", [lineB]);

        // Group membership and review selections must use the persisted order-line identity, never
        // an offer/price id: the line id is the domain-owned key Fulfillment will bind shipments to.
        var groupA = OrderShippingGroup.Create(
            Plan(GroupA, SellerA, RowA, lineA.LineId),
            CheckoutId,
            orderA.SellerOrderId,
            Now);
        groupA.AddLine(lineA.LineId);
        var groupB = OrderShippingGroup.Create(
            Plan(
                GroupB,
                SellerB,
                RowB,
                lineB.LineId,
                shiftGroupBSlot ? OpenTicks + 2L * TimeSpan.TicksPerHour : null),
            CheckoutId,
            orderB.SellerOrderId,
            Now);
        groupB.AddLine(lineB.LineId);

        var checkout = CheckoutGroup.Submit(
            CheckoutId,
            $"tb253r3-{CheckoutId:N}",
            CartId,
            OrderMode.OnlinePurchase,
            Buyer,
            Buyer,
            "IR",
            "IRR",
            SalesChannel.Marketplace,
            [orderA, orderB],
            Now,
            "گیرنده آزمایشی",
            "09120000000",
            "تهران",
            "تهران",
            "خیابان آزمایش ۱",
            "1234567890",
            shippingMethodCode: "post",
            shippingMethodLabel: "پیشتاز",
            shippingAmount: 250_000m,
            minimumDeliveryDate: Day,
            requestedDeliveryDate: Day,
            requestedDeliveryTimeWindow: "14:00–15:00",
            shippingGroups: [groupA, groupB]);
        foreach (var order in checkout.SellerOrders)
        {
            order.RecordVerifiedPayment();
        }

        db.Checkouts.Add(checkout);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return new SeededLines(lineA.LineId, lineB.LineId, packedA, packedB);
    }

    private sealed record SeededLines(Guid LineA, Guid LineB, decimal PackedA, decimal PackedB);

    /// <summary>
    /// One accepted Marketplace group with a committed physical dispatch-origin context — the real
    /// origin seam. Owner identity stays Marketplace; responsibility is never used as origin.
    /// </summary>
    private static OrderShippingGroupPlan Plan(
        Guid groupId,
        Guid sellerPartyId,
        Guid rateRowId,
        Guid lineId,
        long? openOverride = null,
        string? originContext = null)
    {
        var evidence = ShippingGroupAcceptedEvidence.Create(
            SnapshotId,
            DraftId,
            3,
            1,
            1,
            ShippingGroupOwner.Marketplace(),
            AreaGroup,
            rateRowId,
            ShippingTypeId,
            120_000m,
            5_000m,
            "IRR",
            125_000m,
            0m,
            Day,
            openOverride ?? OpenTicks,
            (openOverride ?? OpenTicks) + (CloseTicks - OpenTicks),
            "Asia/Tehran",
            1,
            3);
        return new OrderShippingGroupPlan(
            groupId,
            sellerPartyId,
            "post",
            "post",
            "standard",
            "پیشتاز",
            originContext,
            125_000m,
            "IRR",
            Day,
            Day,
            "14:00–15:00",
            1,
            [lineId],
            evidence);
    }

    private static async Task<OrderDbContext> ReadyAsync(OrderEvidenceRelationalTestTarget target)
    {
        var db = NewOrderContext(target);
        await db.Database.MigrateAsync();
        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM \"order\".order_operational_group_occurrence_members;"
            + "DELETE FROM \"order\".order_operational_group_occurrences;"
            + "DELETE FROM \"order\".order_shipping_group_lines;"
            + "DELETE FROM \"order\".order_shipping_groups;"
            + "DELETE FROM \"order\".checkout_processes;"
            + "DELETE FROM \"order\".order_lines;"
            + "DELETE FROM \"order\".seller_orders;"
            + "DELETE FROM \"order\".checkouts;"
            + "DELETE FROM \"order\".cart_shipping_group_lines;"
            + "DELETE FROM \"order\".cart_shipping_groups;"
            + "DELETE FROM \"order\".cart_shipping_drafts;");
        db.ChangeTracker.Clear();
        return db;
    }

    private static OrderDbContext NewOrderContext(OrderEvidenceRelationalTestTarget target)
    {
        var builder = new DbContextOptionsBuilder<OrderDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            builder,
            target.ConnectionString,
            OrderDbContext.Schema,
            typeof(OrderDbContext));
        return new OrderDbContext(builder.Options);
    }

    private static async Task<FulfillmentDbContext> ReadyFulfillmentAsync(
        OrderEvidenceRelationalTestTarget target)
    {
        var db = NewFulfillmentContext(target);
        await db.Database.MigrateAsync();
        await db.ConsolidatedPackageMembers.ExecuteDeleteAsync();
        await db.ConsolidatedPackages.ExecuteDeleteAsync();
        await db.ShipmentItems.ExecuteDeleteAsync();
        await db.Shipments.ExecuteDeleteAsync();
        await db.Items.ExecuteDeleteAsync();
        await db.Fulfillments.ExecuteDeleteAsync();
        // Fulfillment keeps its own shipping-group rows keyed by the committed Order group ids; a stale row
        // would collide on pk_shipping_groups when the same committed checkout is seeded again.
        await db.ShippingGroups.ExecuteDeleteAsync();
        await db.PaymentInbox.ExecuteDeleteAsync();
        db.ChangeTracker.Clear();
        return db;
    }

    private static FulfillmentDbContext NewFulfillmentContext(
        OrderEvidenceRelationalTestTarget target,
        SaveChangesInterceptor? interceptor = null)
    {
        var builder = new DbContextOptionsBuilder<FulfillmentDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            builder,
            target.ConnectionString,
            FulfillmentDbContext.Schema,
            typeof(FulfillmentDbContext));
        if (interceptor is not null)
        {
            builder.AddInterceptors(interceptor);
        }

        return new FulfillmentDbContext(builder.Options);
    }

    private sealed record Fixture(
        MarketplaceOperationalGroupWorkflow Workflow,
        IReadOnlyList<OrderCommittedCarrierShipmentFacts> Facts,
        IReadOnlyDictionary<Guid, OrderShippingGroupHistoryRecord> History);

    /// <summary>Fails the first SaveChanges that would persist a common package — the real persistence
    /// boundary between committed constituent parcels and the package.</summary>
    private sealed class FailFirstPackageSaveInterceptor : SaveChangesInterceptor
    {
        public int Remaining { get; set; } = 1;

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var context = eventData.Context;
            if (Remaining > 0
                && context is not null
                && context.ChangeTracker.Entries().Any(e =>
                    e.State == EntityState.Added
                    && e.Entity.GetType().Name == "ConsolidatedPackage"))
            {
                Remaining--;
                throw new InvalidOperationException(
                    "TB253R3 injected transport failure before the common package was persisted.");
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }

    /// <summary>
    /// Controlled origin seam. The Order domain deliberately refuses to persist an unverified origin
    /// context (<c>order.shipping.group.origin.unverified</c>), so no committed Order row can carry one
    /// today. Positive grouping facts therefore inject the committed origin field through the same
    /// history contract the production mapper reads; everything else (members, lines, money, slots,
    /// accepted evidence, fulfillment rows) stays real.
    /// </summary>
    private sealed class OriginSeedingHistoryReader(
        IOrderShippingGroupHistoryReader inner,
        Func<Guid, string?> origin)
        : IOrderShippingGroupHistoryReader
    {
        public async Task<IReadOnlyList<OrderShippingGroupHistoryRecord>> GetForCheckoutAsync(
            Guid checkoutId,
            CancellationToken cancellationToken)
        {
            var records = await inner.GetForCheckoutAsync(checkoutId, cancellationToken);
            return records
                .Select(r => r with { OriginContext = origin(r.ShippingGroupId) })
                .ToArray();
        }
    }

    private sealed class ControlledAccessReader(bool allowManage) : IOrderAdminEffectiveAccessReader
    {
        // An ops-family grant is required for granular enforcement: with no ops grant at all the
        // legacy admin path allows everything, so the denial case needs a real non-managing grant.
        public Task<OrderAdminEffectiveAccess> GetAsync(Guid actorUserId, CancellationToken cancellationToken) =>
            Task.FromResult(new OrderAdminEffectiveAccess(
                allowManage
                    ? [new OrderAdminPermissionGrant("fulfillment.manage", false)]
                    : [new OrderAdminPermissionGrant("order.view", false)]));
    }

    private sealed class NoopOrderReader : IOrderFulfillmentReader
    {
        public Task<OrderFulfillmentHandoffSnapshot?> GetHandoffAsync(
            Guid sellerOrderId,
            CancellationToken cancellationToken) =>
            Task.FromResult<OrderFulfillmentHandoffSnapshot?>(null);

        public Task<OrderFulfillmentHandoffSnapshot?> GetHandoffForCheckoutAsync(
            Guid checkoutId,
            Guid actorUserId,
            CancellationToken cancellationToken) =>
            Task.FromResult<OrderFulfillmentHandoffSnapshot?>(null);
    }

    private sealed class NoopInventoryGateway : IFulfillmentInventoryGateway
    {
        public Task ConsumeReservationAsync(Guid reservationId, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task CommitReservationForPaidOrderAsync(Guid reservationId, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
