using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application.Fulfillments.Models;
using Tooba.Fulfillment.Application.Fulfillments.Ports;
using Tooba.Fulfillment.Contracts.Operations;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Fulfillment.Infrastructure.Adapters;
using Tooba.Fulfillment.Infrastructure.Directories;
using Tooba.Fulfillment.Infrastructure.Observability;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Policies;
using Tooba.Order.Application.Admin.Operations.Ports;
using Tooba.Order.Application.Admin.Operations.Services;
using Tooba.Order.Application.Storefront.Ports;
using Tooba.Order.Contracts.Admin.Operations;
using Tooba.Order.Contracts.Fulfillment;
using Tooba.Order.Contracts.Payments;
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
using Tooba.Offer.Contracts.Dtos;
using Tooba.Payment.Contracts.Admin;
using Tooba.Persistence;
using Tooba.Returns.Contracts.Operations;
using Tooba.Settlement.Contracts.Operations;
using Xunit;
using Xunit.Abstractions;
using ShipmentLineCommand = Tooba.Fulfillment.Contracts.Operations.ShipmentLineCommand;

namespace Tooba.Order.Tests.Admin;

/// <summary>
/// TB-259 on the ONE real database: inside the SAME server-owned compatible operational delivery group
/// the operator deliberately chooses ONE common package («common») or INDEPENDENT member parcels
/// («separate»), and that choice is part of the immutable durable occurrence envelope.
/// </summary>
/// <remarks>
/// <para>
/// Every committed row (checkout, seller orders, lines, accepted groups, packed quantities, parcels) is
/// created through the owning domain/application boundaries and read back through the real facts/history
/// readers and the real admin adapter — no hand-built group facts.
/// </para>
/// <para>
/// Nothing is deleted and no user row is touched: each case seeds its own task-labelled checkout with
/// fresh identities. The access reader is the one controlled seam because platform grants live in Host
/// AccessControl, not in Order.
/// </para>
/// </remarks>
public sealed class MarketplaceOperationalPackingChoice259Tests
{
    private const string EnvVar = "TB259_CONNECTION";

    private const string PostMetadata =
        """{"recipientName":"TB259 گیرنده","recipientPhone":"09120000000","postalCode":"1234567890","destinationAddress":"نشانی آزمون تسک ۲۵۹"}""";

    private const string TipaxMetadata =
        """{"recipientName":"TB259 گیرنده","recipientPhone":"09120000000","fullAddress":"نشانی آزمون تسک ۲۵۹"}""";

    private static readonly DateOnly Day = new(2026, 11, 12);
    private static readonly long OpenTicks = 9L * TimeSpan.TicksPerHour;
    private static readonly long CloseTicks = 10L * TimeSpan.TicksPerHour;
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 11, 0, 0, TimeSpan.Zero);
    private static readonly string Origin = "TB259-WH / Tehran";

    private readonly ITestOutputHelper _output;

    public MarketplaceOperationalPackingChoice259Tests(ITestOutputHelper output) => _output = output;

    // ------------------------------------------------------------ A. one common package

    [Fact]
    [Trait("Category", "TB259")]
    [Trait("Case", "A-common")]
    public async Task A_common_choice_creates_the_exact_constituent_parcels_and_one_shared_package()
    {
        if (Target() is not { } target) return;

        await using var fixture = await Fixture.CreateAsync(target, "A", _output);
        var occurrence = Guid.NewGuid();
        var request = fixture.Review(occurrence, "common");

        var first = Json(await fixture.Workflow.PrepareAsync(
            fixture.CheckoutId, request, fixture.Actor, CancellationToken.None));

        Assert.False(first.GetProperty("replayed").GetBoolean());
        Assert.Equal("common", first.GetProperty("packingMode").GetString());
        var packageId = first.GetProperty("consolidatedPackageId").GetGuid();
        var ids = first.GetProperty("shipmentIds").EnumerateArray()
            .Select(x => x.GetGuid()).OrderBy(x => x).ToArray();
        Assert.Equal(2, ids.Length);
        Assert.Equal(2, await fixture.ShipmentsAsync());
        Assert.Equal(1, await fixture.PackagesAsync());
        Assert.Equal(2, await fixture.PackageMembersAsync());

        var recorded = await fixture.Occurrences.GetAsync(occurrence, CancellationToken.None);
        Assert.NotNull(recorded);
        Assert.True(recorded!.Completed);
        Assert.Equal(OperationalGroupPackingMode.Common, recorded.PackingMode);
        Assert.Equal(packageId, recorded.ConsolidatedPackageId);
        // A common envelope stays byte-identical to the TB-253-R4 envelope, so already recorded
        // occurrences keep their historical fingerprint.
        Assert.DoesNotContain("packingMode", recorded.EnvelopeJson, StringComparison.Ordinal);

        var replay = Json(await fixture.Workflow.PrepareAsync(
            fixture.CheckoutId, request, fixture.Actor, CancellationToken.None));
        Assert.True(replay.GetProperty("replayed").GetBoolean());
        Assert.Equal(packageId, replay.GetProperty("consolidatedPackageId").GetGuid());
        Assert.Equal(
            ids,
            replay.GetProperty("shipmentIds").EnumerateArray()
                .Select(x => x.GetGuid()).OrderBy(x => x).ToArray());
        Assert.Equal(2, await fixture.ShipmentsAsync());
        Assert.Equal(1, await fixture.PackagesAsync());

        _output.WriteLine(
            $"A common checkout={fixture.CheckoutId} occurrence={occurrence} parcels={ids.Length} "
            + $"packages=1 package={packageId}");
    }

    // ------------------------------------------------------------ B. independent parcels

    [Fact]
    [Trait("Category", "TB259")]
    [Trait("Case", "B-separate")]
    public async Task B_separate_choice_creates_independent_parcels_and_the_public_command_answers_the_recorded_occurrence_after_readiness_is_gone()
    {
        if (Target() is not { } target) return;

        await using var fixture = await Fixture.CreateAsync(target, "B", _output);
        var occurrence = Guid.NewGuid();
        var request = fixture.Review(occurrence, "separate");

        var result = Json(await fixture.Workflow.PrepareAsync(
            fixture.CheckoutId, request, fixture.Actor, CancellationToken.None));
        var ids = result.GetProperty("shipmentIds").EnumerateArray()
            .Select(x => x.GetGuid()).OrderBy(x => x).ToArray();

        Assert.Equal("separate", result.GetProperty("packingMode").GetString());
        Assert.Equal(JsonValueKind.Null, result.GetProperty("consolidatedPackageId").ValueKind);
        Assert.Equal(2, ids.Length);
        // No physical packaging was invented: every constituent parcel stays independent.
        Assert.Equal(0, await fixture.PackagesAsync());
        Assert.Equal(0, await fixture.PackageMembersAsync());

        var recorded = await fixture.Occurrences.GetAsync(occurrence, CancellationToken.None);
        Assert.NotNull(recorded);
        Assert.True(recorded!.Completed);
        Assert.Null(recorded.ConsolidatedPackageId);
        Assert.Null(recorded.PackageNumber);
        Assert.Equal(OperationalGroupPackingMode.Separate, recorded.PackingMode);
        Assert.Contains("\"packingMode\":\"separate\"", recorded.EnvelopeJson, StringComparison.Ordinal);
        Assert.Equal(2, recorded.Allocations.Count);

        // The independent parcels own their ordinary lifecycle: tracking, dispatch.
        foreach (var parcel in ids)
        {
            var snapshot = await fixture.Adapter.ListForCheckoutAsync(
                fixture.CheckoutId, CancellationToken.None);
            var fulfillmentId = snapshot
                .Single(x => x.Shipments.Any(s => s.ShipmentId == parcel)).FulfillmentId;
            await fixture.Adapter.AssignAutomaticTrackingAsync(
                fulfillmentId, parcel, fixture.Actor, CancellationToken.None);
            await fixture.Adapter.DispatchShipmentAsync(
                fulfillmentId, parcel, fixture.Actor, CancellationToken.None);
        }

        fixture.FulfillmentDb.ChangeTracker.Clear();
        Assert.Equal(0, await fixture.PackagesAsync());

        // Fresh readiness is gone: the projection no longer offers the workflow action at all.
        var page = await fixture.Orchestrator.ListAsync(
            fixture.CheckoutId, fixture.Actor, CancellationToken.None);
        Assert.True(page.IsSuccess, page.IsSuccess ? string.Empty : page.FirstError?.Code);
        Assert.DoesNotContain(
            page.Value.Actions,
            action => action.Code == "prepare_operational_group");

        // The PUBLIC command still answers the EXACT recorded occurrence from its own durable links.
        var replay = await fixture.Orchestrator.PrepareOperationalGroupAsync(
            fixture.CheckoutId, fixture.Actor, request, CancellationToken.None);
        Assert.True(replay.IsSuccess, replay.IsSuccess ? string.Empty : replay.FirstError?.Code);
        var body = Json(replay.Value);
        Assert.True(body.GetProperty("replayed").GetBoolean());
        Assert.Equal("separate", body.GetProperty("packingMode").GetString());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("consolidatedPackageId").ValueKind);
        Assert.Equal(
            ids,
            body.GetProperty("shipmentIds").EnumerateArray()
                .Select(x => x.GetGuid()).OrderBy(x => x).ToArray());

        fixture.FulfillmentDb.ChangeTracker.Clear();
        Assert.Equal(2, await fixture.ShipmentsAsync());
        Assert.Equal(0, await fixture.PackagesAsync());

        // An ABSENT occurrence is never a bypass: a brand-new occurrence must pass the ordinary
        // projection gate, which is no longer ready, so nothing is allocated.
        var fresh = fixture.Review(Guid.NewGuid(), "separate");
        var refused = await fixture.Orchestrator.PrepareOperationalGroupAsync(
            fixture.CheckoutId, fixture.Actor, fresh, CancellationToken.None);
        Assert.False(refused.IsSuccess);
        Assert.Equal("order.operation.invalid", refused.FirstError.Code);
        Assert.Equal(2, await fixture.ShipmentsAsync());
        Assert.Equal(0, await fixture.PackagesAsync());

        _output.WriteLine(
            $"B separate checkout={fixture.CheckoutId} occurrence={occurrence} parcels={ids.Length} "
            + "packages=0 publicReplay=ok(gate would refuse) absentOccurrence=order.operation.invalid");
    }

    // ------------------------------------------------------------ C. immutable choice

    [Fact]
    [Trait("Category", "TB259")]
    [Trait("Case", "C-conflict")]
    public async Task C_same_occurrence_changed_choice_or_quantity_is_a_typed_conflict_with_zero_new_rows()
    {
        if (Target() is not { } target) return;

        await using var fixture = await Fixture.CreateAsync(target, "C", _output);
        var occurrence = Guid.NewGuid();
        var separate = fixture.Review(occurrence, "separate");

        await fixture.Workflow.PrepareAsync(
            fixture.CheckoutId, separate, fixture.Actor, CancellationToken.None);
        fixture.FulfillmentDb.ChangeTracker.Clear();
        var shipments = await fixture.ShipmentsAsync();
        Assert.Equal(2, shipments);

        // Same occurrence, different physical packing choice: grouped evidence is identical, so the
        // CHOICE itself is what the durable envelope must protect.
        var changedMode = await Assert.ThrowsAsync<AdminOrderOperationsException>(() =>
            fixture.Workflow.PrepareAsync(
                fixture.CheckoutId,
                fixture.Review(occurrence, "common"),
                fixture.Actor,
                CancellationToken.None));
        Assert.Equal(MarketplaceOperationalDeliveryGroupAuthority.WorkflowIntentConflict, changedMode.Code);

        var changedQuantity = await Assert.ThrowsAsync<AdminOrderOperationsException>(() =>
            fixture.Workflow.PrepareAsync(
                fixture.CheckoutId,
                fixture.Review(occurrence, "separate", quantity: 2m),
                fixture.Actor,
                CancellationToken.None));
        Assert.Equal(MarketplaceOperationalDeliveryGroupAuthority.WorkflowIntentConflict, changedQuantity.Code);

        // The same changed choice is refused through the PUBLIC command too.
        var publicConflict = await fixture.Orchestrator.PrepareOperationalGroupAsync(
            fixture.CheckoutId,
            fixture.Actor,
            fixture.Review(occurrence, "common"),
            CancellationToken.None);
        Assert.False(publicConflict.IsSuccess);
        Assert.Equal(
            MarketplaceOperationalDeliveryGroupAuthority.WorkflowIntentConflict,
            publicConflict.FirstError.Code);

        fixture.FulfillmentDb.ChangeTracker.Clear();
        Assert.Equal(shipments, await fixture.ShipmentsAsync());
        Assert.Equal(0, await fixture.PackagesAsync());
        var recorded = await fixture.Occurrences.GetAsync(occurrence, CancellationToken.None);
        Assert.NotNull(recorded);
        Assert.Equal(OperationalGroupPackingMode.Separate, recorded!.PackingMode);

        // An unknown token is a typed refusal before any claim/write — never a silent fallback.
        var unknown = await Assert.ThrowsAsync<AdminOrderOperationsException>(() =>
            fixture.Workflow.PrepareAsync(
                fixture.CheckoutId,
                fixture.Review(Guid.NewGuid(), "both"),
                fixture.Actor,
                CancellationToken.None));
        Assert.Equal(
            MarketplaceOperationalDeliveryGroupAuthority.WorkflowPackingModeInvalid,
            unknown.Code);

        fixture.FulfillmentDb.ChangeTracker.Clear();
        Assert.Equal(shipments, await fixture.ShipmentsAsync());
        Assert.Equal(0, await fixture.PackagesAsync());

        _output.WriteLine(
            $"C conflict checkout={fixture.CheckoutId} occurrence={occurrence} changedMode=conflict "
            + $"changedQuantity=conflict publicChangedMode=conflict unknownToken=workflow_packing_mode_invalid rows=2");
    }

    // ------------------------------------------------------------ C2. crash between member writes

    [Theory]
    [InlineData("separate")]
    [InlineData("common")]
    [Trait("Category", "TB259")]
    [Trait("Case", "C2-partial-recovery")]
    public async Task C2_a_failure_between_member_writes_recovers_the_exact_recorded_ids_for_both_completion_shapes(
        string mode)
    {
        if (Target() is not { } target) return;

        await using var fixture = await Fixture.CreateAsync(target, $"C2{mode}", _output);
        var occurrence = Guid.NewGuid();
        var request = fixture.Review(occurrence, mode);

        // Real fault seam: the FIRST constituent parcel is created and linked, then the link of the second
        // member throws. Nothing is rolled back on purpose — that is the crash the retry must survive.
        var faulty = fixture.WorkflowWithStore(new FaultyOccurrenceStore(fixture.Occurrences, failOnMemberCall: 2));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            faulty.PrepareAsync(fixture.CheckoutId, request, fixture.Actor, CancellationToken.None));

        fixture.FulfillmentDb.ChangeTracker.Clear();
        var partial = await fixture.Occurrences.GetAsync(occurrence, CancellationToken.None);
        Assert.NotNull(partial);
        Assert.False(partial!.Completed);
        Assert.Single(partial.Allocations);
        var recordedFirst = partial.Allocations[0].CreatedShipmentIds.Single();
        // Both constituent parcels are already durable: the second create committed and only its durable
        // link is missing — exactly the "crash between create and link" shape.
        Assert.Equal(2, await fixture.ShipmentsAsync());

        // Retry with the SAME occurrence/envelope (the real workflow, the real store).
        var resumed = Json(await fixture.Workflow.PrepareAsync(
            fixture.CheckoutId, request, fixture.Actor, CancellationToken.None));
        var ids = resumed.GetProperty("shipmentIds").EnumerateArray()
            .Select(x => x.GetGuid()).OrderBy(x => x).ToArray();
        Assert.Equal(2, ids.Length);
        Assert.Contains(recordedFirst, ids); // the already-committed member is replayed, never re-created

        fixture.FulfillmentDb.ChangeTracker.Clear();
        Assert.Equal(2, await fixture.ShipmentsAsync());
        var completed = await fixture.Occurrences.GetAsync(occurrence, CancellationToken.None);
        Assert.NotNull(completed);
        Assert.True(completed!.Completed);
        Assert.Equal(2, completed.Allocations.Count);
        Assert.Equal(
            OperationalGroupPackingModes.Separate == mode
                ? OperationalGroupPackingMode.Separate
                : OperationalGroupPackingMode.Common,
            completed.PackingMode);

        if (mode == "separate")
        {
            Assert.Null(completed.ConsolidatedPackageId);
            Assert.Equal(JsonValueKind.Null, resumed.GetProperty("consolidatedPackageId").ValueKind);
            Assert.Equal(0, await fixture.PackagesAsync());
        }
        else
        {
            Assert.NotNull(completed.ConsolidatedPackageId);
            Assert.Equal(completed.ConsolidatedPackageId, resumed.GetProperty("consolidatedPackageId").GetGuid());
            Assert.Equal(1, await fixture.PackagesAsync());
            Assert.Equal(2, await fixture.PackageMembersAsync());
        }

        // The completed operation now replays its own recorded ids with zero new rows.
        var replay = Json(await fixture.Workflow.PrepareAsync(
            fixture.CheckoutId, request, fixture.Actor, CancellationToken.None));
        Assert.True(replay.GetProperty("replayed").GetBoolean());
        Assert.Equal(ids, replay.GetProperty("shipmentIds").EnumerateArray()
            .Select(x => x.GetGuid()).OrderBy(x => x).ToArray());
        fixture.FulfillmentDb.ChangeTracker.Clear();
        Assert.Equal(2, await fixture.ShipmentsAsync());

        _output.WriteLine(
            $"C2 {mode} checkout={fixture.CheckoutId} occurrence={occurrence} linked=1 parcels=2 "
            + $"replayedFirst={recordedFirst} packages={(mode == "separate" ? 0 : 1)}");
    }

    // ------------------------------------------------------------ C3. two independent contexts

    [Fact]
    [Trait("Category", "TB259")]
    [Trait("Case", "C3-two-contexts")]
    public async Task C3_two_independent_contexts_on_one_occurrence_allocate_exactly_one_parcel_set()
    {
        if (Target() is not { } target) return;

        await using var fixture = await Fixture.CreateAsync(target, "C3", _output);
        var occurrence = Guid.NewGuid();
        var request = fixture.Review(occurrence, "separate");

        await using var second = fixture.IndependentCaller();

        // Both callers act on the SAME occurrence/envelope from their OWN contexts. The durable claim and
        // the Fulfillment shippingDispatchKey make this ONE parcel set, never two.
        var firstCall = fixture.Workflow.PrepareAsync(
            fixture.CheckoutId, request, fixture.Actor, CancellationToken.None);
        var secondCall = second.Workflow.PrepareAsync(
            fixture.CheckoutId, request, fixture.Actor, CancellationToken.None);
        var results = await Task.WhenAll(firstCall, secondCall);

        var firstIds = Json(results[0]).GetProperty("shipmentIds").EnumerateArray()
            .Select(x => x.GetGuid()).OrderBy(x => x).ToArray();
        var secondIds = Json(results[1]).GetProperty("shipmentIds").EnumerateArray()
            .Select(x => x.GetGuid()).OrderBy(x => x).ToArray();
        Assert.Equal(2, firstIds.Length);
        Assert.Equal(firstIds, secondIds);

        fixture.FulfillmentDb.ChangeTracker.Clear();
        Assert.Equal(2, await fixture.ShipmentsAsync());
        Assert.Equal(0, await fixture.PackagesAsync());
        var recorded = await fixture.Occurrences.GetAsync(occurrence, CancellationToken.None);
        Assert.NotNull(recorded);
        Assert.True(recorded!.Completed);
        Assert.Equal(OperationalGroupPackingMode.Separate, recorded.PackingMode);
        Assert.Null(recorded.ConsolidatedPackageId);
        Assert.Equal(2, recorded.Allocations.Count);

        _output.WriteLine(
            $"C3 concurrency checkout={fixture.CheckoutId} occurrence={occurrence} parcels=2 packages=0 "
            + $"ids={string.Join(",", firstIds)}");
    }

    // ------------------------------------------------------------ D. authority

    [Fact]
    [Trait("Category", "TB259")]
    [Trait("Case", "D-authority")]
    public async Task D_foreign_group_foreign_reuse_store_ownership_and_unauthorized_actor_refuse_before_writes()
    {
        if (Target() is not { } target) return;

        await using var fixture = await Fixture.CreateAsync(target, "D", _output);
        var seeded = fixture.Seeded;

        // A sibling group of the same seller is not a member of the operational group.
        var foreignGroup = await Assert.ThrowsAsync<AdminOrderOperationsException>(() =>
            fixture.Workflow.PrepareAsync(
                fixture.CheckoutId,
                fixture.Review(Guid.NewGuid(), "common", groups: [fixture.GroupA1, fixture.GroupA2]),
                fixture.Actor,
                CancellationToken.None));
        Assert.Equal(MarketplaceOperationalDeliveryGroupAuthority.WorkflowGroupForeign, foreignGroup.Code);

        // A sibling group of the same seller is NOT a member of this operational group: reuse ownership is
        // resolved by the parcel's own ShippingGroupId, never by the shared sellerOrder.
        var siblingParcel = (await fixture.Adapter.CreateShipmentAsync(
            fixture.FulfillmentForA,
            fixture.Actor,
            "تیپاکس",
            [new ShipmentLineCommand(seeded.LineA2, 1m)],
            "tipax",
            TipaxMetadata,
            CancellationToken.None,
            fixture.GroupA2)).CreatedShipmentId;
        var foreignReuse = await Assert.ThrowsAsync<AdminOrderOperationsException>(() =>
            fixture.Workflow.PrepareAsync(
                fixture.CheckoutId,
                fixture.Review(Guid.NewGuid(), "separate", reuse: [siblingParcel]),
                fixture.Actor,
                CancellationToken.None));
        Assert.Equal(
            MarketplaceOperationalDeliveryGroupAuthority.WorkflowShipmentForeign,
            foreignReuse.Code);

        // Store-owned accepted groups never project into a cross-seller Marketplace operational group, so
        // the request is refused at the projection boundary — before any claim or allocation.
        await using var store = await Fixture.CreateAsync(target, "D-store", _output, storeOwned: true);
        var storeRefusal = await Assert.ThrowsAsync<AdminOrderOperationsException>(() =>
            store.Workflow.PrepareAsync(
                store.CheckoutId,
                store.Review(Guid.NewGuid(), "separate"),
                store.Actor,
                CancellationToken.None));
        Assert.Equal(
            MarketplaceOperationalDeliveryGroupAuthority.WorkflowGroupForeign,
            storeRefusal.Code);
        store.FulfillmentDb.ChangeTracker.Clear();
        Assert.Equal(0, await store.ShipmentsAsync());

        // An actor without order.handle/fulfillment.manage is denied before any allocation.
        await using var denied = await Fixture.CreateAsync(target, "D-denied", _output, allowManage: false);
        var unauthorized = await Assert.ThrowsAsync<AdminOrderOperationsException>(() =>
            denied.Workflow.PrepareAsync(
                denied.CheckoutId,
                denied.Review(Guid.NewGuid(), "common"),
                denied.Actor,
                CancellationToken.None));
        Assert.Equal("order.operation.denied", unauthorized.Code);
        denied.FulfillmentDb.ChangeTracker.Clear();
        Assert.Equal(0, await denied.ShipmentsAsync());
        Assert.Equal(0, await denied.OrderDb.OperationalGroupOccurrences.CountAsync(x => x.CheckoutId == denied.CheckoutId));

        fixture.FulfillmentDb.ChangeTracker.Clear();
        // Only the deliberately seeded independent parcel of group B exists; no package was written.
        Assert.Equal(1, await fixture.ShipmentsAsync());
        Assert.Equal(0, await fixture.PackagesAsync());
        Assert.Equal(0, await fixture.OrderDb.OperationalGroupOccurrences.CountAsync(x => x.CheckoutId == fixture.CheckoutId));

        _output.WriteLine(
            $"D authority checkout={fixture.CheckoutId} foreignGroup=workflow_foreign "
            + "foreignReuse=workflow_shipment_foreign "
            + "storeOwned=workflow_foreign denied=order.operation.denied "
            + $"storeCheckout={store.CheckoutId}");
    }

    // ------------------------------------------------------------ infrastructure

    private static NpgsqlConnectionStringBuilder? Target()
    {
        var connection = Environment.GetEnvironmentVariable(EnvVar);
        return string.IsNullOrWhiteSpace(connection) ? null : new NpgsqlConnectionStringBuilder(connection);
    }

    private static JsonElement Json(object value) =>
        JsonDocument.Parse(JsonSerializer.Serialize(value)).RootElement;

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly ITestOutputHelper _output;

        private Fixture(
            ITestOutputHelper output,
            OrderDbContext orderDb,
            FulfillmentDbContext fulfillmentDb,
            FulfillmentAdminOperationsAdapter adapter,
            MarketplaceOperationalGroupWorkflow workflow,
            AdminOrderOperationsOrchestrator orchestrator,
            OrderOperationalGroupOccurrenceStore occurrences,
            Guid checkoutId,
            Guid actor,
            Guid groupA1,
            Guid groupA2,
            Guid groupB,
            Guid fulfillmentForA,
            Guid fulfillmentForB,
            SeededLines seeded,
            NpgsqlConnectionStringBuilder target,
            IOrderCommittedCarrierShipmentFactsReader facts,
            IOrderShippingGroupHistoryReader history,
            IOrderAdminEffectiveAccessReader access,
            bool allowManage)
        {
            _output = output;
            OrderDb = orderDb;
            FulfillmentDb = fulfillmentDb;
            Adapter = adapter;
            Workflow = workflow;
            Orchestrator = orchestrator;
            Occurrences = occurrences;
            CheckoutId = checkoutId;
            Actor = actor;
            GroupA1 = groupA1;
            GroupA2 = groupA2;
            GroupB = groupB;
            FulfillmentForA = fulfillmentForA;
            FulfillmentForB = fulfillmentForB;
            Seeded = seeded;
            Target = target;
            Facts = facts;
            History = history;
            Access = access;
            AllowManage = allowManage;
        }

        public OrderDbContext OrderDb { get; }
        public FulfillmentDbContext FulfillmentDb { get; }
        public FulfillmentAdminOperationsAdapter Adapter { get; }
        public MarketplaceOperationalGroupWorkflow Workflow { get; }
        public AdminOrderOperationsOrchestrator Orchestrator { get; }
        public OrderOperationalGroupOccurrenceStore Occurrences { get; }
        public Guid CheckoutId { get; }
        public Guid Actor { get; }
        public Guid GroupA1 { get; }
        public Guid GroupA2 { get; }
        public Guid GroupB { get; }
        public Guid FulfillmentForA { get; }
        public Guid FulfillmentForB { get; }
        public SeededLines Seeded { get; }
        public NpgsqlConnectionStringBuilder Target { get; }
        public IOrderCommittedCarrierShipmentFactsReader Facts { get; }
        public IOrderShippingGroupHistoryReader History { get; }
        public IOrderAdminEffectiveAccessReader Access { get; }
        public bool AllowManage { get; }

        /// <summary>The same owning workflow over a different occurrence store (fault/decorator seams).</summary>
        public MarketplaceOperationalGroupWorkflow WorkflowWithStore(IOrderOperationalGroupOccurrenceStore store) =>
            new(Adapter, Facts, History, Access, store);

        /// <summary>
        /// An INDEPENDENT context pair (its own Order/Fulfillment DbContext + store) over the same database,
        /// so two real callers can race on one occurrence exactly as two requests would.
        /// </summary>
        public IndependentCaller IndependentCaller()
        {
            var order = NewOrderContext(Target);
            var fulfillment = NewFulfillmentContext(Target);
            var directory = CreateDirectory(
                fulfillment,
                new Tooba.Order.Infrastructure.Integrations.Fulfillment.OrderFulfillmentBridge(order));
            var adapter = new FulfillmentAdminOperationsAdapter(
                directory,
                new FulfillmentShippedQuantityReader(fulfillment),
                new ShippingMethodsOptions());
            var facts = new OrderCommittedCarrierShipmentFactsReader(order, adapter);
            IOrderShippingGroupHistoryReader history =
                new CommittedOriginHistoryReader(new OrderShippingGroupHistoryReader(order), Origin);
            var store = new OrderOperationalGroupOccurrenceStore(order, new SystemUtcClock());
            return new IndependentCaller(
                new MarketplaceOperationalGroupWorkflow(
                    adapter,
                    facts,
                    history,
                    new ControlledAccessReader(AllowManage),
                    store),
                order,
                fulfillment);
        }

        /// <summary>
        /// Parcels of THIS fixture only: the canonical database is shared, so a bare table count
        /// would include every unrelated row already committed there.
        /// </summary>
        public Task<int> ShipmentsAsync() =>
            FulfillmentDb.Shipments.CountAsync(x =>
                x.FulfillmentId == FulfillmentForA || x.FulfillmentId == FulfillmentForB);

        /// <summary>Consolidated packages of THIS checkout only.</summary>
        public Task<int> PackagesAsync() =>
            FulfillmentDb.ConsolidatedPackages.CountAsync(x => x.CheckoutId == CheckoutId);

        /// <summary>Package memberships of THIS fixture's fulfillments only.</summary>
        public Task<int> PackageMembersAsync() =>
            FulfillmentDb.ConsolidatedPackageMembers.CountAsync(x =>
                x.FulfillmentId == FulfillmentForA || x.FulfillmentId == FulfillmentForB);

        /// <summary>The reviewed request: exact member groups, per-line quantities and packing choice.</summary>
        public AdminOrderOperationRequest Review(
            Guid occurrence,
            string packingMode,
            decimal quantity = 1m,
            Guid[]? groups = null,
            Guid[]? reuse = null,
            bool includeB = true) =>
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
                includeB
                    ? [
                        new AdminOrderLineSelection(Seeded.LineA1, quantity),
                        new AdminOrderLineSelection(Seeded.LineB, quantity),
                    ]
                    : [
                        new AdminOrderLineSelection(Seeded.LineA1, quantity),
                        new AdminOrderLineSelection(Seeded.LineA2, quantity),
                    ],
                null,
                null,
                null,
                null,
                null,
                OperationalGroupShippingGroupIds: groups ?? [GroupA1, GroupB],
                ReuseShipmentIds: reuse,
                OperationalGroupOccurrenceId: occurrence,
                OperationalGroupPackingMode: packingMode);

        public ValueTask DisposeAsync()
        {
            OrderDb.Dispose();
            FulfillmentDb.Dispose();
            return ValueTask.CompletedTask;
        }

        public static async Task<Fixture> CreateAsync(
            NpgsqlConnectionStringBuilder target,
            string tag,
            ITestOutputHelper output,
            bool allowManage = true,
            bool storeOwned = false)
        {
            var checkoutId = Guid.NewGuid();
            var cartId = Guid.NewGuid();
            var buyer = Guid.NewGuid();
            var actor = Guid.NewGuid();
            var sellerA = Guid.NewGuid();
            var sellerB = Guid.NewGuid();
            var orderA = Guid.NewGuid();
            var orderB = Guid.NewGuid();
            // Line ids are owned by the entity; the fixture always references the created ids.
            var groupA1 = Guid.NewGuid();
            var groupA2 = Guid.NewGuid();
            var groupB = Guid.NewGuid();
            var snapshotId = Guid.NewGuid();
            var draftId = Guid.NewGuid();
            var areaGroup = Guid.NewGuid();
            var shippingTypeId = Guid.NewGuid();

            // The fixture owns both contexts for the whole case; they are disposed by the fixture.
            var orderDb = NewOrderContext(target);
            var fulfillmentDb = NewFulfillmentContext(target);
            await orderDb.Database.MigrateAsync();
            await fulfillmentDb.Database.MigrateAsync();

            // A distinct owner instance per group: an owned instance must never be shared by two owners.
            ShippingGroupOwner NewOwner() => storeOwned
                ? ShippingGroupOwner.Store(tenantKey: "store-alpha")
                : ShippingGroupOwner.Marketplace();

            var lineA1Entity = OrderLine.FromCheckout(
                orderA, Guid.NewGuid(), Guid.NewGuid(), sellerA, 1m, 50_000m, "IRR", true,
                Guid.NewGuid(), Guid.NewGuid(), "Taxable", 0.09m, 4_500m, 54_500m, null);
            var lineA2Entity = OrderLine.FromCheckout(
                orderA, Guid.NewGuid(), Guid.NewGuid(), sellerA, 1m, 40_000m, "IRR", true,
                Guid.NewGuid(), Guid.NewGuid(), "Taxable", 0.09m, 3_600m, 43_600m, null);
            var lineBEntity = OrderLine.FromCheckout(
                orderB, Guid.NewGuid(), Guid.NewGuid(), sellerB, 1m, 30_000m, "IRR", true,
                Guid.NewGuid(), Guid.NewGuid(), "Taxable", 0.09m, 2_700m, 32_700m, null);
            var lineA1 = lineA1Entity.LineId;
            var lineA2 = lineA2Entity.LineId;
            var lineB = lineBEntity.LineId;
            var sellerOrderA = SellerOrder.Open(
                checkoutId, sellerA, $"TB259{tag}A{Guid.NewGuid():N}"[..20],
                OrderMode.OnlinePurchase, "IRR", [lineA1Entity, lineA2Entity]);
            var sellerOrderB = SellerOrder.Open(
                checkoutId, sellerB, $"TB259{tag}B{Guid.NewGuid():N}"[..20],
                OrderMode.OnlinePurchase, "IRR", [lineBEntity]);

            OrderShippingGroupPlan Plan(Guid groupId, Guid sellerPartyId, string methodCode, Guid lineId) =>
                new(
                    groupId,
                    sellerPartyId,
                    methodCode,
                    methodCode,
                    "standard",
                    methodCode == "post" ? "پیشتاز" : "تیپاکس",
                    null,
                    125_000m,
                    "IRR",
                    Day,
                    Day,
                    "09:00–10:00",
                    1,
                    [lineId],
                    ShippingGroupAcceptedEvidence.Create(
                        snapshotId, draftId, 3, 1, 1, NewOwner(), areaGroup,
                        Guid.NewGuid(), shippingTypeId,
                        120_000m, 5_000m, "IRR", 125_000m, 0m,
                        Day, OpenTicks, CloseTicks, "Asia/Tehran", 1, 3));

            var groupA1Entity = OrderShippingGroup.Create(
                Plan(groupA1, sellerA, "post", lineA1), checkoutId, orderA, Now);
            output.WriteLine($"DIAG a1 after create={groupA1Entity.AcceptedEvidence?.Owner?.Kind.ToString() ?? "NULL"} sellerOrder={groupA1Entity.SellerOrderId}");
            groupA1Entity.AddLine(lineA1);
            var groupA2Entity = OrderShippingGroup.Create(
                Plan(groupA2, sellerA, "tipax", lineA2), checkoutId, orderA, Now);
            groupA2Entity.AddLine(lineA2);
            var groupBEntity = OrderShippingGroup.Create(
                Plan(groupB, sellerB, "post", lineB), checkoutId, orderB, Now);
            groupBEntity.AddLine(lineB);

            var checkout = CheckoutGroup.Submit(
                checkoutId,
                $"tb259-{tag}-{checkoutId:N}",
                cartId,
                OrderMode.OnlinePurchase,
                buyer,
                buyer,
                "IR",
                "IRR",
                SalesChannel.Marketplace,
                [sellerOrderA, sellerOrderB],
                Now,
                "TB259 گیرنده",
                "09120000000",
                "تهران",
                "تهران",
                $"نشانی آزمون تسک ۲۵۹ {tag}",
                "1234567890",
                shippingMethodCode: "post",
                shippingMethodLabel: "پیشتاز",
                shippingAmount: 250_000m,
                minimumDeliveryDate: Day,
                requestedDeliveryDate: Day,
                requestedDeliveryTimeWindow: "09:00–10:00",
                shippingGroups: [groupA1Entity, groupA2Entity, groupBEntity]);
            foreach (var order in checkout.SellerOrders)
            {
                order.RecordVerifiedPayment();
            }

            orderDb.Checkouts.Add(checkout);
            output.WriteLine("DIAG owners(before save)=" + string.Join(
                ",",
                checkout.ShippingGroups.Select(g =>
                    $"{g.MethodCode}:{g.AcceptedEvidence?.Owner?.Kind.ToString() ?? "NULL"}")));
            await orderDb.SaveChangesAsync();
            orderDb.ChangeTracker.Clear();

            var directory = CreateDirectory(fulfillmentDb, new Tooba.Order.Infrastructure.Integrations.Fulfillment.OrderFulfillmentBridge(orderDb));
            // Fresh payment/event identity: a reused event id is treated as already processed.
            await directory.CreateFromPaidSellerOrdersAsync(
                Guid.NewGuid(), Guid.NewGuid(), [orderA, orderB], CancellationToken.None);
            foreach (var row in new[]
            {
                (OrderId: orderA, Lines: new[] { (lineA1, 1m), (lineA2, 1m) }),
                (OrderId: orderB, Lines: new[] { (lineB, 1m) }),
            })
            {
                var unit = await directory.GetBySellerOrderAsync(row.OrderId, CancellationToken.None);
                Assert.NotNull(unit);
                await directory.MarkProcessingAsync(unit!.FulfillmentId, actor, CancellationToken.None);
                await directory.PackSelectionsAsync(
                    unit.FulfillmentId,
                    actor,
                    row.Lines.Select(x =>
                            new Tooba.Fulfillment.Application.Fulfillments.Models.FulfillmentSelectionCommand(
                                x.Item1, x.Item2))
                        .ToArray(),
                    CancellationToken.None);
            }

            fulfillmentDb.ChangeTracker.Clear();

            var adapter = new FulfillmentAdminOperationsAdapter(
                directory,
                new FulfillmentShippedQuantityReader(fulfillmentDb),
                new ShippingMethodsOptions());
            var factsReader = new OrderCommittedCarrierShipmentFactsReader(orderDb, adapter);
            IOrderShippingGroupHistoryReader historyReader =
                new CommittedOriginHistoryReader(new OrderShippingGroupHistoryReader(orderDb), Origin);
            var access = new ControlledAccessReader(allowManage);
            var occurrences = new OrderOperationalGroupOccurrenceStore(orderDb, new SystemUtcClock());
            var workflow = new MarketplaceOperationalGroupWorkflow(
                adapter, factsReader, historyReader, access, occurrences);
            var orchestrator = new AdminOrderOperationsOrchestrator(
                new AdminOrderOperationsCheckoutReader(orderDb),
                adapter,
                new ReturnsFake(),
                null!,
                access,
                new PaymentFake(),
                new PaymentProjectionFake(),
                new SettlementFake(),
                new InventoryRecoveryFake(),
                new SupplyFake(),
                factsReader,
                historyReader,
                occurrences);

            var snapshots = await adapter.ListForCheckoutAsync(checkoutId, CancellationToken.None);
            var fulfillmentForA = snapshots.Single(x => x.SellerOrderId == orderA).FulfillmentId;
            var fulfillmentForB = snapshots.Single(x => x.SellerOrderId == orderB).FulfillmentId;

            return new Fixture(
                output,
                orderDb,
                fulfillmentDb,
                adapter,
                workflow,
                orchestrator,
                occurrences,
                checkoutId,
                actor,
                groupA1,
                groupA2,
                groupB,
                fulfillmentForA,
                fulfillmentForB,
                new SeededLines(lineA1, lineA2, lineB),
                target,
                factsReader,
                historyReader,
                access,
                allowManage);
        }
    }

    /// <summary>UUIDv7-shaped line identity, so order lines keep their production layout.</summary>
    private static Guid NewLineId()
    {
        Span<byte> bytes = stackalloc byte[16];
        Random.Shared.NextBytes(bytes);
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x70);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes);
    }

    private static OrderDbContext NewOrderContext(NpgsqlConnectionStringBuilder target)
    {
        var builder = new DbContextOptionsBuilder<OrderDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            builder, target.ConnectionString, OrderDbContext.Schema, typeof(OrderDbContext));
        return new OrderDbContext(builder.Options);
    }

    private static FulfillmentDbContext NewFulfillmentContext(NpgsqlConnectionStringBuilder target)
    {
        var builder = new DbContextOptionsBuilder<FulfillmentDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            builder, target.ConnectionString, FulfillmentDbContext.Schema, typeof(FulfillmentDbContext));
        return new FulfillmentDbContext(builder.Options);
    }

    private static FulfillmentDirectory CreateDirectory(
        FulfillmentDbContext db,
        IOrderFulfillmentReader orders) =>
        new(
            db,
            new OpenFulfillmentUseCaseGuard(),
            orders,
            new NoopInventoryGateway(),
            new FulfillmentInstrumentation(),
            new SystemUtcClock(),
            new UuidV7IdGenerator());

    private sealed record SeededLines(Guid LineA1, Guid LineA2, Guid LineB);

    /// <summary>Independent context pair + workflow over the SAME canonical database.</summary>
    private sealed class IndependentCaller(
        MarketplaceOperationalGroupWorkflow workflow,
        OrderDbContext order,
        FulfillmentDbContext fulfillment) : IAsyncDisposable
    {
        public MarketplaceOperationalGroupWorkflow Workflow { get; } = workflow;

        public ValueTask DisposeAsync()
        {
            order.Dispose();
            fulfillment.Dispose();
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// Real fault seam: throws on exactly the Nth durable member link, i.e. AFTER the (N−1)th constituent
    /// parcel was created and committed. The retry must therefore reconcile recorded ids, not re-create.
    /// </summary>
    private sealed class FaultyOccurrenceStore(IOrderOperationalGroupOccurrenceStore inner, int failOnMemberCall)
        : IOrderOperationalGroupOccurrenceStore
    {
        private int _memberCalls;

        public Task<OperationalGroupOccurrenceRecord> ClaimAsync(
            OperationalGroupOccurrenceEnvelope envelope,
            string envelopeFingerprint,
            CancellationToken cancellationToken) =>
            inner.ClaimAsync(envelope, envelopeFingerprint, cancellationToken);

        public Task<OperationalGroupOccurrenceAllocation> RecordMemberAllocationAsync(
            Guid occurrenceId,
            OperationalGroupOccurrenceAllocation allocation,
            CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref _memberCalls) == failOnMemberCall)
            {
                throw new InvalidOperationException("TB259 injected fault between the member writes");
            }

            return inner.RecordMemberAllocationAsync(occurrenceId, allocation, cancellationToken);
        }

        public Task RecordCompletedAsync(
            Guid occurrenceId,
            Guid consolidatedPackageId,
            string packageNumber,
            string shippingMethodCode,
            CancellationToken cancellationToken) =>
            inner.RecordCompletedAsync(occurrenceId, consolidatedPackageId, packageNumber, shippingMethodCode, cancellationToken);

        public Task RecordCompletedSeparateAsync(
            Guid occurrenceId,
            string shippingMethodCode,
            CancellationToken cancellationToken) =>
            inner.RecordCompletedSeparateAsync(occurrenceId, shippingMethodCode, cancellationToken);

        public Task<OperationalGroupOccurrenceRecord?> GetAsync(
            Guid occurrenceId,
            CancellationToken cancellationToken) =>
            inner.GetAsync(occurrenceId, cancellationToken);
    }

    /// <summary>
    /// Committed physical dispatch origin. The Order domain refuses an unverified origin, so the
    /// committed origin field is supplied through the same history contract the mapper reads;
    /// everything else (members, lines, money, slots, accepted evidence) stays a real persisted row.
    /// </summary>
    private sealed class CommittedOriginHistoryReader(
        IOrderShippingGroupHistoryReader inner,
        string origin) : IOrderShippingGroupHistoryReader
    {
        public async Task<IReadOnlyList<OrderShippingGroupHistoryRecord>> GetForCheckoutAsync(
            Guid checkoutId,
            CancellationToken cancellationToken)
        {
            var records = await inner.GetForCheckoutAsync(checkoutId, cancellationToken);
            return records.Select(r => r with { OriginContext = origin }).ToArray();
        }
    }

    private sealed class ControlledAccessReader(bool allowManage) : IOrderAdminEffectiveAccessReader
    {
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

    private sealed class ReturnsFake : IReturnAdminOperations
    {
        public Task<IReadOnlyList<ReturnSnapshot>> ListBySellerOrderIdsAsync(
            IReadOnlyList<Guid> sellerOrderIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ReturnSnapshot>>([]);

        public Task<IReadOnlyList<ReturnStatusOverlayRow>> ListStatusOverlayAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ReturnStatusOverlayRow>>([]);

        public Task<ReturnEligibilityResult> EvaluateEligibilityAsync(
            Guid sellerOrderId,
            CancellationToken cancellationToken) =>
            Task.FromResult(new ReturnEligibilityResult(
                sellerOrderId, Guid.Empty, false, "return.not_delivered", null, null, []));

        public Task<ReturnSnapshot> CreateAdminInitiatedAsync(
            CreateAdminReturnCommand command,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<ReturnSnapshot> ApproveAsync(
            Guid returnRequestId, Guid actorUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ReturnSnapshot> RejectAsync(
            Guid returnRequestId,
            Guid actorUserId,
            string? reason,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<ReturnSnapshot> RetryRefundAsync(
            Guid returnRequestId, Guid actorUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<CourierOriginReturnResult> CreateCourierOriginAsync(
            CreateCourierOriginReturnCommand command,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<CourierOriginReturnResult?> ReconcileCourierOriginAsync(
            Guid fulfillmentId,
            Guid shipmentId,
            string occurrenceKey,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class PaymentFake : IPaymentAdminGateway
    {
        public Task<PaymentAdminOperationalSnapshot?> GetLatestOperationalForCheckoutAsync(
            Guid checkoutId, CancellationToken cancellationToken) =>
            Task.FromResult<PaymentAdminOperationalSnapshot?>(null);

        public Task CloseOrStartRefundForOrderCancelAsync(
            Guid checkoutId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task RestoreAfterOrderCancelRestoreAsync(
            Guid checkoutId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<PaymentAdminMutationResult> ConfirmDepositAsync(
            Guid paymentId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<PaymentAdminMutationResult> RejectDepositAsync(
            Guid paymentId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<PaymentAdminMutationResult> RestoreDepositAsync(
            Guid paymentId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<PaymentAdminMutationResult> UnconfirmDepositAsync(
            Guid paymentId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class PaymentProjectionFake : IOrderPaymentProjectionPort
    {
        public Task ApplyVerifiedSuccessAsync(
            Guid checkoutId,
            Guid paymentId,
            IReadOnlyList<Guid> sellerOrderIds,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task RevertVerifiedSuccessAsync(
            Guid checkoutId,
            IReadOnlyList<Guid> sellerOrderIds,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task PromoteReservationsForManualPaymentReviewAsync(
            Guid checkoutId,
            DateTimeOffset reviewExpiresAt,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task ReleaseReservationsAfterManualRejectAsync(
            Guid checkoutId,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class SettlementFake : ISettlementOrderAccrualPort
    {
        public Task<IReadOnlyDictionary<Guid, SellerOrderRestoreSettlementGate>> GetRestoreGatesAsync(
            IReadOnlyList<Guid> sellerOrderIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, SellerOrderRestoreSettlementGate>>(
                new Dictionary<Guid, SellerOrderRestoreSettlementGate>());

        public Task NeutralizeUnpaidAccrualForCancelAsync(
            Guid paymentId,
            IReadOnlyList<Guid> sellerOrderIds,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task ReinstateAccrualAfterCancelRestoreAsync(
            Guid paymentId,
            IReadOnlyList<Guid> sellerOrderIds,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task VoidUnpaidAccrualForPaymentAsync(
            Guid paymentId,
            IReadOnlyList<Guid> sellerOrderIds,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class InventoryRecoveryFake : IAdminOrderOperationsInventoryRecoveryPort
    {
        public Task<AdminOrderInventoryRecoveryAssessment> AssessAsync(
            Guid checkoutId, CancellationToken cancellationToken) =>
            Task.FromResult(new AdminOrderInventoryRecoveryAssessment(checkoutId, "A", false, string.Empty));

        public Task<AdminOrderInventoryRecoveryResult> RecoverAsync(
            Guid checkoutId,
            Guid actorUserId,
            string? reason,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class SupplyFake : IAdminOrderOperationsSupplyPort
    {
        public Task<AdminOrderOpsSupplyStatus> GetStatusAsync(
            Guid checkoutId, CancellationToken cancellationToken) =>
            Task.FromResult(new AdminOrderOpsSupplyStatus(checkoutId, "Available", string.Empty, []));

        public Task<AdminOrderOpsSupplyEnsureResult> EnsurePaidDurableAsync(
            Guid checkoutId,
            string reason,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
