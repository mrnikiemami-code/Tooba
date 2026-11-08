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
using Xunit;
using ShipmentLineCommand = Tooba.Fulfillment.Contracts.Operations.ShipmentLineCommand;

namespace Tooba.Order.Tests.Admin;

/// <summary>
/// TB-253-R4: the immutable occurrence envelope, explicit reuse-only execution and prebound
/// request preservation, proven on the REAL Order + Fulfillment stack over a disposable PostgreSQL.
/// </summary>
/// <remarks>
/// <para>
/// Every committed row (checkout, seller orders, lines, accepted shipping groups, packed quantities,
/// parcels) is created through the owning domain/application boundaries and read back through the
/// production facts/history readers and admin adapter — no hand-built group facts.
/// </para>
/// <para>
/// One seller deliberately owns TWO accepted groups inside ONE seller order. That is what makes the
/// reuse-ownership defect observable: an identity resolved from <c>sellerOrderId</c> alone cannot
/// distinguish the two, while the shipment's own <c>ShippingGroupId</c> can.
/// </para>
/// </remarks>
[Collection(OrderEvidenceRelationalCollection.Name)]
public sealed class MarketplaceOperationalGroupOccurrenceTests
{
    private const string EnvVar = "TOOBA_TB253R4_ORDER_POSTGRES";
    private const string ExpectedDatabase = "order_tb253r4";
    private const string ExpectedUser = "tb253r4";

    private static readonly Guid CheckoutId = Guid.Parse("01a1c254-0000-7000-8000-0000000000c1");
    private static readonly Guid CartId = Guid.Parse("01a1c254-0000-7000-8000-0000000000a1");
    private static readonly Guid Buyer = Guid.Parse("01a1c254-0000-7000-8000-0000000000b1");
    private static readonly Guid Actor = Guid.Parse("01a1c254-0000-7000-8000-0000000000d1");
    private static readonly Guid SellerA = Guid.Parse("01a1c254-0000-7000-8000-0000000000e1");
    private static readonly Guid SellerB = Guid.Parse("01a1c254-0000-7000-8000-0000000000e2");
    private static readonly Guid OrderA = Guid.Parse("01a1c254-0000-7000-8000-0000000000f1");
    private static readonly Guid OrderB = Guid.Parse("01a1c254-0000-7000-8000-0000000000f2");
    private static readonly Guid LineA1 = Guid.Parse("01a1c254-0000-7000-8000-000000000101");
    private static readonly Guid LineA2 = Guid.Parse("01a1c254-0000-7000-8000-000000000102");
    private static readonly Guid LineB = Guid.Parse("01a1c254-0000-7000-8000-000000000103");
    private static readonly Guid GroupA1 = Guid.Parse("01a1c254-0000-7000-8000-000000000201");
    private static readonly Guid GroupA2 = Guid.Parse("01a1c254-0000-7000-8000-000000000202");
    private static readonly Guid GroupB = Guid.Parse("01a1c254-0000-7000-8000-000000000203");
    private static readonly Guid SnapshotId = Guid.Parse("01a1c254-0000-7000-8000-000000000301");
    private static readonly Guid DraftId = Guid.Parse("01a1c254-0000-7000-8000-000000000302");
    private static readonly Guid AreaGroup = Guid.Parse("01a1c254-0000-7000-8000-000000000401");
    private static readonly Guid RowA1 = Guid.Parse("01a1c254-0000-7000-8000-000000000501");
    private static readonly Guid RowA2 = Guid.Parse("01a1c254-0000-7000-8000-000000000502");
    private static readonly Guid RowB = Guid.Parse("01a1c254-0000-7000-8000-000000000503");
    private static readonly Guid ShippingTypeId = Guid.Parse("01a1c254-0000-7000-8000-000000000601");
    private static readonly string Origin = "WH-R4 / Tehran";
    private static readonly DateOnly Day = new(2026, 11, 11);
    private static readonly long OpenTicks = 9L * TimeSpan.TicksPerHour;
    private static readonly long CloseTicks = 10L * TimeSpan.TicksPerHour;
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 10, 0, 0, TimeSpan.Zero);

    private static Guid Occurrence(string suffix) =>
        Guid.Parse($"01a1c254-0000-7000-8000-0000000009{suffix}");

    /// <summary>Trusted recipient metadata for a real post shipment (validator requires it).</summary>
    private const string PostMetadata =
        """{"recipientName":"گیرنده آزمایشی","recipientPhone":"09120000000","postalCode":"1234567890","destinationAddress":"خیابان آزمایش ۲"}""";

    /// <summary>Trusted recipient metadata for a real tipax shipment (validator requires it).</summary>
    private const string TipaxMetadata =
        """{"recipientName":"گیرنده آزمایشی","recipientPhone":"09120000000","fullAddress":"خیابان آزمایش ۲"}""";

    // ------------------------------------------------------------ A. explicit reuse-only execution

    [Fact]
    [Trait("Category", "TB253R4")]
    [Trait("Case", "A-reuse-only")]
    public async Task A_two_reuse_only_members_create_the_package_with_zero_new_parcels()
    {
        var target = Target();
        await using var orderDb = await ReadyAsync(target);
        await using var fulfillmentDb = await ReadyFulfillmentAsync(target);
        var seeded = await SeedCommittedOrderAsync(orderDb, fulfillmentDb);
        var adapter = CreateAdapter(fulfillmentDb);
        var fulfillment = await LoadFulfillmentIdsAsync(adapter);

        // Two real, pre-existing constituent parcels — one per accepted group, not yet packaged.
        var parcelA1 = (await adapter.CreateShipmentAsync(
            fulfillment[OrderA],
            Actor,
            "پیشتاز",
            [new ShipmentLineCommand(seeded.LineA1, 1m)],
            "post",
            PostMetadata,
            CancellationToken.None,
            GroupA1,
            "tb253r4-seed-a1")).CreatedShipmentId;
        var parcelB = (await adapter.CreateShipmentAsync(
            fulfillment[OrderB],
            Actor,
            "پیشتاز",
            [new ShipmentLineCommand(seeded.LineB, 1m)],
            "post",
            PostMetadata,
            CancellationToken.None,
            GroupB,
            "tb253r4-seed-b")).CreatedShipmentId;

        var fixture = await CreateFixtureAsync(orderDb, fulfillmentDb);
        var occurrence = Occurrence("11");

        // Zero line selections: the member holds ONLY reusable parcels. Calling the create binder here
        // would invent a selection or refuse an exhausted member, so reuse-only must never reach it.
        var request = ReviewRequest(
            occurrence,
            [],
            reuse: [parcelA1, parcelB]);

        var result = Json(await fixture.Workflow.PrepareAsync(CheckoutId, request, Actor, CancellationToken.None));

        Assert.False(result.GetProperty("replayed").GetBoolean());
        var members = result.GetProperty("members").EnumerateArray().ToArray();
        Assert.Equal(2, members.Length);
        Assert.All(members, m =>
        {
            Assert.True(m.GetProperty("reused").GetBoolean());
            Assert.False(m.GetProperty("created").GetBoolean());
            Assert.Single(m.GetProperty("shipmentIds").EnumerateArray());
        });

        fulfillmentDb.ChangeTracker.Clear();
        Assert.Equal(2, await fulfillmentDb.Shipments.CountAsync());
        Assert.Equal(1, await fulfillmentDb.ConsolidatedPackages.CountAsync());
        Assert.Equal(2, await fulfillmentDb.ConsolidatedPackageMembers.CountAsync());
        Assert.Equal(2, await fulfillmentDb.ShipmentItems.CountAsync());

        var packageId = result.GetProperty("consolidatedPackageId").GetGuid();
        var package = (await adapter.GetPackagesForCheckoutAsync(CheckoutId, CancellationToken.None))
            .Single(p => p.ConsolidatedPackageId == packageId);
        Assert.True(
            new[] { parcelA1, parcelB }.OrderBy(x => x).ToArray()
                .SequenceEqual(package.Members.Select(m => m.ShipmentId).OrderBy(x => x).ToArray()));

        // The immutable envelope recorded exactly the reviewed reuse identity.
        var recorded = await new OrderOperationalGroupOccurrenceStore(orderDb, new SystemUtcClock())
            .GetAsync(occurrence, CancellationToken.None);
        Assert.NotNull(recorded);
        Assert.True(recorded!.Completed);
        Assert.Equal(2, recorded.Allocations.Count);
        Assert.All(recorded.Allocations, a => Assert.Empty(a.CreatedShipmentIds));
        Assert.True(
            new[] { parcelA1, parcelB }.OrderBy(x => x).ToArray()
                .SequenceEqual(recorded.Allocations
                    .SelectMany(a => a.ReusedShipmentIds)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToArray()));

        // Replaying the SAME occurrence is answered from recorded links only.
        var replay = Json(await fixture.Workflow.PrepareAsync(CheckoutId, request, Actor, CancellationToken.None));
        Assert.True(replay.GetProperty("replayed").GetBoolean());
        Assert.Equal(packageId, replay.GetProperty("consolidatedPackageId").GetGuid());
        fulfillmentDb.ChangeTracker.Clear();
        Assert.Equal(2, await fulfillmentDb.Shipments.CountAsync());
    }

    // ------------------------------------------------------------ B. group-scoped reuse ownership

    [Fact]
    [Trait("Category", "TB253R4")]
    [Trait("Case", "B-group-scoped-reuse")]
    public async Task B_a_parcel_of_a_sibling_group_of_the_same_seller_is_foreign_not_reused()
    {
        var target = Target();
        await using var orderDb = await ReadyAsync(target);
        await using var fulfillmentDb = await ReadyFulfillmentAsync(target);
        var seeded = await SeedCommittedOrderAsync(orderDb, fulfillmentDb);
        var adapter = CreateAdapter(fulfillmentDb);
        var fulfillment = await LoadFulfillmentIdsAsync(adapter);

        // Seller A owns TWO groups in ONE seller order; this parcel belongs to the sibling group.
        var siblingParcel = (await adapter.CreateShipmentAsync(
            fulfillment[OrderA],
            Actor,
            "تیپاکس",
            [new ShipmentLineCommand(seeded.LineA2, 1m)],
            "tipax",
            TipaxMetadata,
            CancellationToken.None,
            GroupA2,
            "tb253r4-seed-a2")).CreatedShipmentId;

        var fixture = await CreateFixtureAsync(orderDb, fulfillmentDb);

        // sellerOrderId-only resolution would have accepted this parcel for GroupA1 (same seller order);
        // the shipment's own ShippingGroupId proves it belongs to a group that is NOT a member.
        var error = await Assert.ThrowsAsync<AdminOrderOperationsException>(() =>
            fixture.Workflow.PrepareAsync(
                CheckoutId,
                ReviewRequest(
                    Occurrence("21"),
                    [(seeded.LineA1, 1m), (seeded.LineB, 1m)],
                    reuse: [siblingParcel]),
                Actor,
                CancellationToken.None));

        Assert.Equal(MarketplaceOperationalDeliveryGroupAuthority.WorkflowShipmentForeign, error.Code);
        fulfillmentDb.ChangeTracker.Clear();
        Assert.Equal(1, await fulfillmentDb.Shipments.CountAsync());
        Assert.Equal(0, await fulfillmentDb.ConsolidatedPackages.CountAsync());
        Assert.Equal(0, await orderDb.OperationalGroupOccurrences.CountAsync());
    }

    // ------------------------------------------------------------ C. crash between members

    [Fact]
    [Trait("Category", "TB253R4")]
    [Trait("Case", "C-member-crash")]
    public async Task C_crash_after_the_first_member_retry_finishes_that_exact_parcel_and_a_changed_request_conflicts()
    {
        var target = Target();
        await using var orderDb = await ReadyAsync(target);
        await using var fulfillmentDb = await ReadyFulfillmentAsync(target);
        var seeded = await SeedCommittedOrderAsync(orderDb, fulfillmentDb);

        var occurrence = Occurrence("31");
        var request = ReviewRequest(
            occurrence,
            [(seeded.LineA1, 1m), (seeded.LineB, 1m)]);

        // Fail the SECOND constituent parcel persistence: member A is already durably linked while
        // member B's create never commits — the exact "crash between members" window.
        var interceptor = new FailSecondShipmentSaveInterceptor();
        await using (var failingDb = NewFulfillmentContext(target, interceptor))
        {
            var failing = await CreateFixtureAsync(orderDb, failingDb);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                failing.Workflow.PrepareAsync(CheckoutId, request, Actor, CancellationToken.None));
            Assert.Equal(0, interceptor.Remaining);
        }

        fulfillmentDb.ChangeTracker.Clear();
        var afterCrash = await fulfillmentDb.Shipments.AsNoTracking().ToListAsync();
        Assert.Single(afterCrash);
        var firstParcel = afterCrash.Single().ShipmentId;
        Assert.Equal(0, await fulfillmentDb.ConsolidatedPackages.CountAsync());

        var recorded = await new OrderOperationalGroupOccurrenceStore(orderDb, new SystemUtcClock())
            .GetAsync(occurrence, CancellationToken.None);
        Assert.NotNull(recorded);
        Assert.False(recorded!.Completed);
        Assert.Single(recorded.Allocations);
        Assert.Equal(
            firstParcel,
            recorded.Allocations.SelectMany(a => a.CreatedShipmentIds).Single());

        // Retry the SAME occurrence: member A is replayed from its durable link (never re-created),
        // member B is finished, and the package is completed.
        var fixture = await CreateFixtureAsync(orderDb, fulfillmentDb);
        var recovered = Json(await fixture.Workflow.PrepareAsync(CheckoutId, request, Actor, CancellationToken.None));
        var recoveredIds = recovered.GetProperty("shipmentIds")
            .EnumerateArray().Select(x => x.GetGuid()).OrderBy(x => x).ToArray();
        Assert.Equal(2, recoveredIds.Length);
        Assert.Contains(firstParcel, recoveredIds);

        var replayedMember = recovered.GetProperty("members").EnumerateArray()
            .Single(m => m.GetProperty("shippingGroupId").GetGuid() == GroupA1);
        Assert.True(replayedMember.GetProperty("replayed").GetBoolean());
        Assert.False(replayedMember.GetProperty("created").GetBoolean());

        fulfillmentDb.ChangeTracker.Clear();
        Assert.Equal(2, await fulfillmentDb.Shipments.CountAsync());
        Assert.Equal(1, await fulfillmentDb.ConsolidatedPackages.CountAsync());

        // A CHANGED request under the recorded occurrence is a typed conflict — including a change to
        // the member that was never allocated — and writes nothing.
        var conflict = await Assert.ThrowsAsync<AdminOrderOperationsException>(() =>
            fixture.Workflow.PrepareAsync(
                CheckoutId,
                ReviewRequest(occurrence, [(seeded.LineA1, 1m), (seeded.LineB, 2m)]),
                Actor,
                CancellationToken.None));
        Assert.Equal(MarketplaceOperationalDeliveryGroupAuthority.WorkflowIntentConflict, conflict.Code);
        fulfillmentDb.ChangeTracker.Clear();
        Assert.Equal(2, await fulfillmentDb.Shipments.CountAsync());
        Assert.Equal(1, await fulfillmentDb.ConsolidatedPackages.CountAsync());

        // A DIFFERENT actor cannot inherit a recorded occurrence either.
        var foreignActor = await Assert.ThrowsAsync<AdminOrderOperationsException>(() =>
            fixture.Workflow.PrepareAsync(CheckoutId, request, Guid.NewGuid(), CancellationToken.None));
        Assert.Equal(MarketplaceOperationalDeliveryGroupAuthority.WorkflowIntentConflict, foreignActor.Code);
        fulfillmentDb.ChangeTracker.Clear();
        Assert.Equal(2, await fulfillmentDb.Shipments.CountAsync());
    }

    // ------------------------------------------------------------ D. review-line preservation

    [Fact]
    [Trait("Category", "TB253R4")]
    [Trait("Case", "D-preflight")]
    public async Task D_unknown_duplicate_and_foreign_review_members_are_refused_before_the_first_write()
    {
        var target = Target();
        await using var orderDb = await ReadyAsync(target);
        await using var fulfillmentDb = await ReadyFulfillmentAsync(target);
        var seeded = await SeedCommittedOrderAsync(orderDb, fulfillmentDb);
        var fixture = await CreateFixtureAsync(orderDb, fulfillmentDb);

        var duplicate = await Assert.ThrowsAsync<AdminOrderOperationsException>(() =>
            fixture.Workflow.PrepareAsync(
                CheckoutId,
                ReviewRequest(Occurrence("41"), [(seeded.LineA1, 1m), (seeded.LineA1, 1m), (seeded.LineB, 1m)]),
                Actor,
                CancellationToken.None));
        Assert.Equal(MarketplaceOperationalDeliveryGroupAuthority.WorkflowSelectionInvalid, duplicate.Code);

        // A committed line outside the reviewed member set is never silently dropped.
        var siblingLine = await Assert.ThrowsAsync<AdminOrderOperationsException>(() =>
            fixture.Workflow.PrepareAsync(
                CheckoutId,
                ReviewRequest(Occurrence("42"), [(seeded.LineA2, 1m), (seeded.LineB, 1m)]),
                Actor,
                CancellationToken.None));
        Assert.Equal(MarketplaceOperationalDeliveryGroupAuthority.WorkflowSelectionInvalid, siblingLine.Code);

        // An unknown group id cannot be part of any projected member set.
        var foreignGroup = await Assert.ThrowsAsync<AdminOrderOperationsException>(() =>
            fixture.Workflow.PrepareAsync(
                CheckoutId,
                ReviewRequest(
                    Occurrence("44"),
                    [(seeded.LineA1, 1m), (seeded.LineB, 1m)],
                    groups: [GroupA1, Guid.NewGuid()]),
                Actor,
                CancellationToken.None));
        Assert.Equal(MarketplaceOperationalDeliveryGroupAuthority.WorkflowGroupForeign, foreignGroup.Code);

        var overQuantity = await Assert.ThrowsAsync<AdminOrderOperationsException>(() =>
            fixture.Workflow.PrepareAsync(
                CheckoutId,
                ReviewRequest(Occurrence("43"), [(seeded.LineA1, 1m), (seeded.LineB, 9m)]),
                Actor,
                CancellationToken.None));
        Assert.Equal(MarketplaceOperationalDeliveryGroupAuthority.WorkflowSelectionInvalid, overQuantity.Code);

        fulfillmentDb.ChangeTracker.Clear();
        Assert.Equal(0, await fulfillmentDb.Shipments.CountAsync());
        Assert.Equal(0, await fulfillmentDb.ConsolidatedPackages.CountAsync());
        Assert.Equal(0, await orderDb.OperationalGroupOccurrences.CountAsync());
    }

    // ---------------------------------------------------------------- E. real concurrency on the claim path

    [Fact]
    [Trait("Category", "TB253R4")]
    [Trait("Case", "E-concurrent-claim")]
    public async Task E_two_contexts_on_one_barrier_still_allocate_exactly_one_parcel_set()
    {
        var target = Target();
        await using var orderDb = await ReadyAsync(target);
        await using var fulfillmentDb = await ReadyFulfillmentAsync(target);
        // Two units committed and packed per line: the barrier twin that reads facts AFTER its sibling
        // allocated one unit must still project the same member set (a consumed group is not a changed
        // request), so this proves claim idempotency rather than a quantity race.
        var seeded = await SeedCommittedOrderAsync(
            orderDb, fulfillmentDb, committedQuantity: 2m, packedQuantity: 2m);

        var first = await CreateFixtureAsync(orderDb, fulfillmentDb);
        await using var orderDb2 = NewOrderContext(target);
        await using var fulfillmentDb2 = NewFulfillmentContext(target);
        var second = await CreateFixtureAsync(orderDb2, fulfillmentDb2);

        var occurrence = Occurrence("51");
        var request = ReviewRequest(occurrence, [(seeded.LineA1, 1m), (seeded.LineB, 1m)]);

        using var barrier = new Barrier(2);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<object> Run(MarketplaceOperationalGroupWorkflow workflow, CancellationToken cancellationToken)
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
        Assert.Single(roots.Select(r => r.GetProperty("consolidatedPackageId").GetGuid()).Distinct());
        var sets = roots
            .Select(r => r.GetProperty("shipmentIds").EnumerateArray().Select(x => x.GetGuid()).OrderBy(x => x).ToArray())
            .ToArray();
        Assert.Equal(sets[0], sets[1]);
        Assert.Equal(2, sets[0].Length);

        var record = await new OrderOperationalGroupOccurrenceStore(orderDb, new SystemUtcClock())
            .GetAsync(occurrence, CancellationToken.None);
        Assert.NotNull(record);
        Assert.True(record!.Completed);
        Assert.Equal(2, record.Allocations.Count);

        await using var fulfillmentVerify = NewFulfillmentContext(target);
        Assert.Equal(2, await fulfillmentVerify.Shipments.CountAsync());
        Assert.Equal(1, await fulfillmentVerify.ConsolidatedPackages.CountAsync());
        Assert.Equal(2, await fulfillmentVerify.ConsolidatedPackageMembers.CountAsync());
    }

    // ---------------------------------------------------------------- F. mixed create + reuse replay

    [Fact]
    [Trait("Category", "TB253R4")]
    [Trait("Case", "F-mixed-create-reuse")]
    public async Task F_mixed_create_and_reuse_replays_the_same_package_without_creating_a_third_parcel()
    {
        var target = Target();
        await using var orderDb = await ReadyAsync(target);
        await using var fulfillmentDb = await ReadyFulfillmentAsync(target);

        var seeded = await SeedCommittedOrderAsync(orderDb, fulfillmentDb);
        var adapter = CreateAdapter(fulfillmentDb);
        var fulfillment = await LoadFulfillmentIdsAsync(adapter);

        // One REAL pre-existing parcel for group A1 — the member that will be executed reuse-only.
        var parcelA1 = (await adapter.CreateShipmentAsync(
            fulfillment[OrderA],
            Actor,
            "پیشتاز",
            [new ShipmentLineCommand(seeded.LineA1, 1m)],
            "post",
            PostMetadata,
            CancellationToken.None,
            GroupA1,
            "tb253r4-seed-mixed-a1")).CreatedShipmentId;

        var fixture = await CreateFixtureAsync(orderDb, fulfillmentDb);

        var occurrence = Occurrence("61");
        // Line A1 is reused from an existing parcel; Line B must be created.
        var request = ReviewRequest(
            occurrence,
            [(seeded.LineB, 1m)],
            reuse: [parcelA1]);

        var first = Json(await fixture.Workflow.PrepareAsync(CheckoutId, request, Actor, CancellationToken.None));
        Assert.False(first.GetProperty("replayed").GetBoolean());
        var packageId = first.GetProperty("consolidatedPackageId").GetGuid();
        var shipmentIds = first.GetProperty("shipmentIds").EnumerateArray().Select(x => x.GetGuid()).OrderBy(x => x).ToArray();
        Assert.Equal(2, shipmentIds.Length);
        Assert.Contains(parcelA1, shipmentIds);

        var second = Json(await fixture.Workflow.PrepareAsync(CheckoutId, request, Actor, CancellationToken.None));
        Assert.True(second.GetProperty("replayed").GetBoolean());
        Assert.Equal(packageId, second.GetProperty("consolidatedPackageId").GetGuid());
        Assert.Equal(
            shipmentIds,
            second.GetProperty("shipmentIds").EnumerateArray().Select(x => x.GetGuid()).OrderBy(x => x).ToArray());

        var record = await new OrderOperationalGroupOccurrenceStore(orderDb, new SystemUtcClock())
            .GetAsync(occurrence, CancellationToken.None);
        Assert.NotNull(record);
        Assert.True(record!.Completed);
        Assert.Equal(2, record.Allocations.Count);

        await using var fulfillmentVerify = NewFulfillmentContext(target);
        // The reused parcel plus exactly one new parcel — never a duplicate for the reused line.
        Assert.Equal(2, await fulfillmentVerify.Shipments.CountAsync());
        Assert.Equal(1, await fulfillmentVerify.ConsolidatedPackages.CountAsync());
        Assert.Equal(2, await fulfillmentVerify.ConsolidatedPackageMembers.CountAsync());
    }

    // ------------------------------------------------------------ infrastructure

    private static OrderEvidenceRelationalTestTarget Target() =>
        OrderEvidenceRelationalTestTarget.FromEnvironment(EnvVar, ExpectedDatabase, ExpectedUser);

    private static JsonElement Json(object value) =>
        JsonDocument.Parse(JsonSerializer.Serialize(value)).RootElement;

    private static AdminOrderOperationRequest ReviewRequest(
        Guid occurrence,
        (Guid LineId, decimal Quantity)[] lines,
        Guid[]? reuse = null,
        Guid[]? groups = null) =>
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
            OperationalGroupShippingGroupIds: groups ?? [GroupA1, GroupB],
            ReuseShipmentIds: reuse,
            OperationalGroupOccurrenceId: occurrence);

    private static async Task<Dictionary<Guid, Guid>> LoadFulfillmentIdsAsync(
        IFulfillmentAdminOperations adapter)
    {
        var snapshots = await adapter.ListForCheckoutAsync(CheckoutId, CancellationToken.None);
        return snapshots.ToDictionary(x => x.SellerOrderId, x => x.FulfillmentId);
    }

    private static async Task<Fixture> CreateFixtureAsync(
        OrderDbContext orderDb,
        FulfillmentDbContext fulfillmentDb)
    {
        var adapter = CreateAdapter(fulfillmentDb);
        var factsReader = new OrderCommittedCarrierShipmentFactsReader(orderDb, adapter);
        IOrderShippingGroupHistoryReader historyReader =
            new CommittedOriginHistoryReader(new OrderShippingGroupHistoryReader(orderDb), Origin);
        var workflow = new MarketplaceOperationalGroupWorkflow(
            adapter,
            factsReader,
            historyReader,
            new AllowAllAccessReader(),
            new OrderOperationalGroupOccurrenceStore(orderDb, new SystemUtcClock()));
        var facts = await factsReader.ListByCheckoutAsync(CheckoutId, CancellationToken.None);
        var history = await historyReader.GetForCheckoutAsync(CheckoutId, CancellationToken.None);
        return new Fixture(workflow, facts, history.ToDictionary(x => x.ShippingGroupId));
    }

    private static FulfillmentAdminOperationsAdapter CreateAdapter(FulfillmentDbContext fulfillmentDb)
    {
        var directory = CreateFulfillmentDirectory(fulfillmentDb);
        return new FulfillmentAdminOperationsAdapter(
            directory,
            new FulfillmentShippedQuantityReader(fulfillmentDb),
            new ShippingMethodsOptions());
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
        FulfillmentDbContext fulfillmentDb,
        decimal committedQuantity = 1m,
        decimal packedQuantity = 1m)
    {        var lineA1 = OrderLine.FromCheckout(
            OrderA, LineA1, Guid.NewGuid(), SellerA, committedQuantity, 50_000m, "IRR", true, Guid.NewGuid(), Guid.NewGuid(),
            "Taxable", 0.09m, 4_500m, 54_500m, null);
        var lineA2 = OrderLine.FromCheckout(
            OrderA, LineA2, Guid.NewGuid(), SellerA, committedQuantity, 40_000m, "IRR", true, Guid.NewGuid(), Guid.NewGuid(),
            "Taxable", 0.09m, 3_600m, 43_600m, null);
        var lineB = OrderLine.FromCheckout(
            OrderB, LineB, Guid.NewGuid(), SellerB, committedQuantity, 30_000m, "IRR", true, Guid.NewGuid(), Guid.NewGuid(),
            "Taxable", 0.09m, 2_700m, 32_700m, null);
        var orderA = SellerOrder.Open(
            CheckoutId, SellerA, $"TB253R4A{OrderA:N}"[..20], OrderMode.OnlinePurchase, "IRR", [lineA1, lineA2]);
        var orderB = SellerOrder.Open(
            CheckoutId, SellerB, $"TB253R4B{OrderB:N}"[..20], OrderMode.OnlinePurchase, "IRR", [lineB]);

        // One seller, two accepted groups in ONE seller order: post (member of the operational group)
        // and tipax (sibling group whose parcels must never be reused for the other group).
        var groupA1 = OrderShippingGroup.Create(
            Plan(GroupA1, SellerA, RowA1, "post", lineA1.LineId),
            CheckoutId,
            orderA.SellerOrderId,
            Now);
        groupA1.AddLine(lineA1.LineId);
        var groupA2 = OrderShippingGroup.Create(
            Plan(GroupA2, SellerA, RowA2, "tipax", lineA2.LineId),
            CheckoutId,
            orderA.SellerOrderId,
            Now);
        groupA2.AddLine(lineA2.LineId);
        var groupB = OrderShippingGroup.Create(
            Plan(GroupB, SellerB, RowB, "post", lineB.LineId),
            CheckoutId,
            orderB.SellerOrderId,
            Now);
        groupB.AddLine(lineB.LineId);

        var checkout = CheckoutGroup.Submit(
            CheckoutId,
            $"tb253r4-{CheckoutId:N}",
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
            "خیابان آزمایش ۲",
            "1234567890",
            shippingMethodCode: "post",
            shippingMethodLabel: "پیشتاز",
            shippingAmount: 250_000m,
            minimumDeliveryDate: Day,
            requestedDeliveryDate: Day,
            requestedDeliveryTimeWindow: "09:00–10:00",
            shippingGroups: [groupA1, groupA2, groupB]);
        foreach (var order in checkout.SellerOrders)
        {
            order.RecordVerifiedPayment();
        }

        db.Checkouts.Add(checkout);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var seeded = new SeededLines(lineA1.LineId, lineA2.LineId, lineB.LineId);
        await EnsurePackedFulfillmentsAsync(db, fulfillmentDb, seeded, packedQuantity);
        return seeded;
    }

    private static async Task EnsurePackedFulfillmentsAsync(
        OrderDbContext orderDb,
        FulfillmentDbContext fulfillmentDb,
        SeededLines seeded,
        decimal packedQuantity = 1m)
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
        await directory.CreateFromPaidSellerOrdersAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [OrderA, OrderB],
            CancellationToken.None);
        foreach (var row in new[]
        {
            (OrderId: OrderA, Lines: new[] { (seeded.LineA1, packedQuantity), (seeded.LineA2, packedQuantity) }),
            (OrderId: OrderB, Lines: new[] { (seeded.LineB, packedQuantity) }),
        })
        {
            var unit = await directory.GetBySellerOrderAsync(row.OrderId, CancellationToken.None);
            Assert.NotNull(unit);
            await directory.MarkProcessingAsync(unit!.FulfillmentId, Actor, CancellationToken.None);
            await directory.PackSelectionsAsync(
                unit.FulfillmentId,
                Actor,
                row.Lines
                    .Select(x => new Tooba.Fulfillment.Application.Fulfillments.Models.FulfillmentSelectionCommand(
                        x.Item1,
                        x.Item2))
                    .ToArray(),
                CancellationToken.None);
        }

        fulfillmentDb.ChangeTracker.Clear();
    }

    private static OrderShippingGroupPlan Plan(
        Guid groupId,
        Guid sellerPartyId,
        Guid rateRowId,
        string methodCode,
        Guid lineId)
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
            OpenTicks,
            CloseTicks,
            "Asia/Tehran",
            1,
            3);
        return new OrderShippingGroupPlan(
            groupId,
            sellerPartyId,
            methodCode,
            methodCode,
            "standard",
            "پیشتاز",
            null,
            125_000m,
            "IRR",
            Day,
            Day,
            "09:00–10:00",
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
        Tooba.Persistence.ToobaNpgsql.ConfigureModuleContext(
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
        Tooba.Persistence.ToobaNpgsql.ConfigureModuleContext(
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

    private sealed record SeededLines(Guid LineA1, Guid LineA2, Guid LineB);

    /// <summary>Fails the SECOND persisted shipment — the crash window between two members.</summary>
    private sealed class FailSecondShipmentSaveInterceptor : SaveChangesInterceptor
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
                    && e.Entity.GetType().Name == "Shipment"))
            {
                // The first parcel has already been committed in a previous SaveChanges call.
                if (Committed > 0)
                {
                    Remaining--;
                    throw new InvalidOperationException(
                        "TB253R4 injected transport failure before the second constituent parcel persisted.");
                }

                Committed++;
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public int Committed { get; private set; }
    }

    /// <summary>
    /// Committed physical dispatch origin. The Order domain refuses an unverified origin context, so
    /// the committed origin field is supplied through the same history contract the mapper reads;
    /// everything else stays a real persisted row.
    /// </summary>
    private sealed class CommittedOriginHistoryReader(
        IOrderShippingGroupHistoryReader inner,
        string origin)
        : IOrderShippingGroupHistoryReader
    {
        public async Task<IReadOnlyList<OrderShippingGroupHistoryRecord>> GetForCheckoutAsync(
            Guid checkoutId,
            CancellationToken cancellationToken)
        {
            var records = await inner.GetForCheckoutAsync(checkoutId, cancellationToken);
            return records.Select(r => r with { OriginContext = origin }).ToArray();
        }
    }

    private sealed class AllowAllAccessReader : IOrderAdminEffectiveAccessReader
    {
        public Task<OrderAdminEffectiveAccess> GetAsync(Guid actorUserId, CancellationToken cancellationToken) =>
            Task.FromResult(new OrderAdminEffectiveAccess(
                [new OrderAdminPermissionGrant("fulfillment.manage", false)]));
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
