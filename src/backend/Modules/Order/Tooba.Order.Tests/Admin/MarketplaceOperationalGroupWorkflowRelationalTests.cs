using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application.Fulfillments.Models;
using Tooba.Fulfillment.Application.Fulfillments.Ports;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Fulfillment.Infrastructure.Adapters;
using Tooba.Fulfillment.Infrastructure.Directories;
using Tooba.Fulfillment.Infrastructure.Observability;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Policies;
using Tooba.Order.Application.Admin.Operations.Services;
using Tooba.Order.Contracts.Admin.Operations;
using Tooba.Order.Contracts.Fulfillment;
using Tooba.Order.Contracts.Shipping;
using Tooba.Order.Domain;
using Tooba.Order.Domain.Aggregates;
using Tooba.Order.Domain.Checkout;
using Tooba.Order.Domain.Enums;
using Tooba.Order.Infrastructure.Integrations.Fulfillment;
using Tooba.Order.Infrastructure.Integrations.Shipping;
using Tooba.Order.Infrastructure.Integrations.Storefront;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Order.Tests.Checkout;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Order.Tests.Admin;

/// <summary>
/// TB-253-R2: the REAL Order path of the Marketplace operational group workflow on the task-owned
/// disposable PostgreSQL — committed Order shipping-group evidence built through the real domain
/// boundaries, the real facts/history readers, the real mapper/authority, the real Fulfillment
/// directory through the production admin adapter, and the public workflow the admin
/// command/orchestrator delegates to.
/// </summary>
/// <remarks>
/// <para>
/// Nothing here hand-crafts a <c>GroupFact</c> or invents a group/line identity: every member, line,
/// owner, receiving slot and fee comes from persisted Order rows and is read back through
/// <see cref="OrderCommittedCarrierShipmentFactsReader"/> and <see cref="OrderShippingGroupHistoryReader"/>.
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
    private const string EnvVar = "TOOBA_TB253R2_ORDER_POSTGRES";
    private const string ExpectedDatabase = "order_tb253r2";
    private const string ExpectedUser = "tb253r2";

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
    private static readonly DateOnly Day = new(2026, 11, 10);
    private static readonly long OpenTicks = 14L * TimeSpan.TicksPerHour;
    private static readonly long CloseTicks = 15L * TimeSpan.TicksPerHour;
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    [Trait("Category", "TB253R2")]
    [Trait("Case", "group-workflow")]
    public async Task Two_compatible_marketplace_sellers_get_group_bound_parcels_and_one_common_package()
    {
        var target = Target();
        await using var orderDb = await ReadyAsync(target);
        await using var fulfillmentDb = await ReadyFulfillmentAsync(target);
        var seeded = await SeedCommittedOrderAsync(orderDb);

        var fixture = await CreateFixtureAsync(orderDb, fulfillmentDb, allowManage: true, seeded);
        Assert.Equal(2, fixture.Facts.Count);
        Assert.Contains(fixture.Facts, x => x.SellerPartyId == SellerA && x.ShippingGroupId == GroupA);
        Assert.Contains(fixture.Facts, x => x.SellerPartyId == SellerB && x.ShippingGroupId == GroupB);
        // Real packed quantity is the parcel-create input; Manual groups keep executable lines even
        // though the carrier-transport reason is not_carrier_api.
        Assert.All(fixture.Facts, f => Assert.NotEmpty(f.ExecutableLines));
        Assert.All(fixture.Facts, f => Assert.False(f.IsCancelled || f.PaymentLocked));

        var result = await fixture.Workflow.PrepareAsync(
            CheckoutId,
            ReviewRequest((seeded.LineA, 2m), (seeded.LineB, 3m)),
            Actor,
            CancellationToken.None);

        var root = Json(result);
        Assert.Equal("prepare_operational_group", root.GetProperty("code").GetString());
        Assert.False(root.GetProperty("replayed").GetBoolean());
        Assert.Equal("post", root.GetProperty("shippingMethodCode").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("operationLocator").GetString()));
        var packageId = root.GetProperty("consolidatedPackageId").GetGuid();

        // Real persisted parcels keep their OWN constituent group identity and reviewed quantities.
        var shipments = await fulfillmentDb.Shipments.AsNoTracking().ToListAsync();
        var shipmentItems = await fulfillmentDb.ShipmentItems.AsNoTracking().ToListAsync();
        Assert.Equal(2, shipments.Count);
        var parcelA = Assert.Single(shipments, x => x.ShippingGroupId == GroupA);
        var parcelB = Assert.Single(shipments, x => x.ShippingGroupId == GroupB);
        Assert.Equal("post", parcelA.ShippingMethodCode);
        Assert.Equal(2m, shipmentItems.Where(x => x.ShipmentId == parcelA.ShipmentId).Sum(x => x.Quantity));
        Assert.Equal(3m, shipmentItems.Where(x => x.ShipmentId == parcelB.ShipmentId).Sum(x => x.Quantity));
        Assert.Equal(seeded.LineA, Assert.Single(shipmentItems, x => x.ShipmentId == parcelA.ShipmentId).OrderLineId);
        Assert.Equal(seeded.LineB, Assert.Single(shipmentItems, x => x.ShipmentId == parcelB.ShipmentId).OrderLineId);

        var package = await fulfillmentDb.ConsolidatedPackages
            .AsNoTracking()
            .SingleAsync(x => x.ConsolidatedPackageId == packageId);
        var packageMembers = await fulfillmentDb.ConsolidatedPackageMembers
            .AsNoTracking()
            .Where(x => x.ConsolidatedPackageId == packageId)
            .ToListAsync();
        Assert.Equal("post", package.ShippingMethodCode);
        Assert.Equal(2, packageMembers.Count);
        Assert.Contains(packageMembers, m => m.ShipmentId == parcelA.ShipmentId);
        Assert.Contains(packageMembers, m => m.ShipmentId == parcelB.ShipmentId);

        // The accepted Order money/evidence and the financial seller orders are untouched.
        orderDb.ChangeTracker.Clear();
        var orders = await orderDb.SellerOrders.AsNoTracking().Include(x => x.Lines).ToListAsync();
        Assert.Equal(2, orders.Count);
        Assert.All(orders, o => Assert.Equal(SellerOrderStatus.Paid, o.Status));
        Assert.Equal(100_000m, orders.Single(x => x.SellerOrderId == OrderA).Lines.Single().LineTotalSnapshot);
        Assert.Equal(120_000m, orders.Single(x => x.SellerOrderId == OrderB).Lines.Single().LineTotalSnapshot);

        var groups = await orderDb.OrderShippingGroups.AsNoTracking().Include(x => x.AcceptedEvidence).ToListAsync();
        Assert.Equal(2, groups.Count);
        Assert.Equal(125_000m, groups.Single(x => x.ShippingGroupId == GroupA).ShippingChargeSnapshot);
        Assert.Equal(125_000m, groups.Single(x => x.ShippingGroupId == GroupB).ShippingChargeSnapshot);
        Assert.All(groups, g => Assert.Equal(SnapshotId, g.AcceptedEvidence!.SnapshotId));
        Assert.All(groups, g => Assert.Equal(ShippingGroupOwnerKind.Marketplace, g.AcceptedEvidence!.Owner.Kind));
    }

    [Fact]
    [Trait("Category", "TB253R2")]
    [Trait("Case", "retry-reuse")]
    public async Task Retry_and_concurrent_twin_reuse_the_recorded_parcels_and_package_without_new_allocation()
    {
        var target = Target();
        await using var orderDb = await ReadyAsync(target);
        await using var fulfillmentDb = await ReadyFulfillmentAsync(target);
        var seeded = await SeedCommittedOrderAsync(orderDb);

        var fixture = await CreateFixtureAsync(orderDb, fulfillmentDb, allowManage: true, seeded);
        var request = ReviewRequest((seeded.LineA, 2m), (seeded.LineB, 3m));

        var first = await fixture.Workflow.PrepareAsync(CheckoutId, request, Actor, CancellationToken.None);
        var firstPackageId = Json(first).GetProperty("consolidatedPackageId").GetGuid();
        var firstShipmentIds = Json(first).GetProperty("shipmentIds")
            .EnumerateArray().Select(x => x.GetGuid()).OrderBy(x => x).ToArray();

        // Same-tick double click / lost response: identical reviewed request, no new allocation.
        var second = Json(await fixture.Workflow.PrepareAsync(
            CheckoutId, request, Actor, CancellationToken.None));
        Assert.True(second.GetProperty("replayed").GetBoolean());
        Assert.Equal(firstPackageId, second.GetProperty("consolidatedPackageId").GetGuid());
        Assert.Equal(
            firstShipmentIds,
            second.GetProperty("shipmentIds").EnumerateArray().Select(x => x.GetGuid()).OrderBy(x => x).ToArray());

        // Concurrent twin after the recorded result: same operation identity, same recorded package.
        var twin = Json(await fixture.Workflow.PrepareAsync(
            CheckoutId, request, Actor, CancellationToken.None));
        Assert.Equal(firstPackageId, twin.GetProperty("consolidatedPackageId").GetGuid());

        Assert.Equal(2, await fulfillmentDb.Shipments.CountAsync());
        Assert.Equal(1, await fulfillmentDb.ConsolidatedPackages.CountAsync());
        Assert.Equal(2, await fulfillmentDb.ConsolidatedPackageMembers.CountAsync());
    }

    [Fact]
    [Trait("Category", "TB253R2")]
    [Trait("Case", "refusals")]
    public async Task Unauthorized_actor_foreign_group_conflicting_method_and_missing_selection_are_typed_refusals()
    {
        var target = Target();
        await using var orderDb = await ReadyAsync(target);
        await using var fulfillmentDb = await ReadyFulfillmentAsync(target);
        var seeded = await SeedCommittedOrderAsync(orderDb);

        // Unauthorized actor: refused before any parcel/package write.
        var denied = await CreateFixtureAsync(orderDb, fulfillmentDb, allowManage: false, seeded);
        var deniedError = await Assert.ThrowsAsync<AdminOrderOperationsException>(() =>
            denied.Workflow.PrepareAsync(
                CheckoutId,
                ReviewRequest((seeded.LineA, 2m), (seeded.LineB, 3m)),
                Actor,
                CancellationToken.None));
        Assert.Equal("order.operation.denied", deniedError.Code);
        Assert.Equal(0, await fulfillmentDb.Shipments.CountAsync());
        Assert.Equal(0, await fulfillmentDb.ConsolidatedPackages.CountAsync());

        var fixture = await CreateFixtureAsync(orderDb, fulfillmentDb, allowManage: true, seeded);

        // Foreign/incomplete member set: not one committed compatible group.
        var foreign = await Assert.ThrowsAsync<AdminOrderOperationsException>(() =>
            fixture.Workflow.PrepareAsync(
                CheckoutId,
                ReviewRequestWithGroups((GroupA, Guid.NewGuid()), (seeded.LineA, 2m), (seeded.LineB, 3m)),
                Actor,
                CancellationToken.None));
        Assert.Equal(MarketplaceOperationalDeliveryGroupAuthority.WorkflowGroupForeign, foreign.Code);

        // Conflicting client method: the workflow owns the accepted method; no override.
        var method = await Assert.ThrowsAsync<AdminOrderOperationsException>(() =>
            fixture.Workflow.PrepareAsync(
                CheckoutId,
                ReviewRequest((seeded.LineA, 2m), (seeded.LineB, 3m)) with { ShippingMethodCode = "tipax" },
                Actor,
                CancellationToken.None));
        Assert.Equal(MarketplaceOperationalDeliveryGroupAuthority.ConsolidateIncompatible, method.Code);

        // No reviewed quantity and no reusable parcel anywhere: typed refusal, nothing allocated.
        var empty = await Assert.ThrowsAsync<AdminOrderOperationsException>(() =>
            fixture.Workflow.PrepareAsync(
                CheckoutId,
                ReviewRequest(),
                Actor,
                CancellationToken.None));
        Assert.Equal(MarketplaceOperationalDeliveryGroupAuthority.WorkflowSelectionRequired, empty.Code);

        Assert.Equal(0, await fulfillmentDb.Shipments.CountAsync());
        Assert.Equal(0, await fulfillmentDb.ConsolidatedPackages.CountAsync());
    }

    [Fact]
    [Trait("Category", "TB253R2")]
    [Trait("Case", "incompatible-slot")]
    public async Task Committed_groups_with_a_different_receiving_hour_are_not_one_operational_group()
    {
        var target = Target();
        await using var orderDb = await ReadyAsync(target);
        await using var fulfillmentDb = await ReadyFulfillmentAsync(target);
        var seeded = await SeedCommittedOrderAsync(orderDb, shiftGroupBSlot: true);

        var fixture = await CreateFixtureAsync(orderDb, fulfillmentDb, allowManage: true, seeded);

        var error = await Assert.ThrowsAsync<AdminOrderOperationsException>(() =>
            fixture.Workflow.PrepareAsync(
                CheckoutId,
                ReviewRequest((seeded.LineA, 2m), (seeded.LineB, 3m)),
                Actor,
                CancellationToken.None));

        Assert.Equal(MarketplaceOperationalDeliveryGroupAuthority.WorkflowGroupForeign, error.Code);
        Assert.Equal(0, await fulfillmentDb.Shipments.CountAsync());
    }

    private static OrderEvidenceRelationalTestTarget Target() =>
        OrderEvidenceRelationalTestTarget.FromEnvironment(EnvVar, ExpectedDatabase, ExpectedUser);

    private static JsonElement Json(object value) =>
        JsonDocument.Parse(JsonSerializer.Serialize(value)).RootElement;

    private static AdminOrderOperationRequest ReviewRequest(params (Guid LineId, decimal Quantity)[] lines) =>
        ReviewRequestWithGroups((GroupA, GroupB), lines);

    private static AdminOrderOperationRequest ReviewRequestWithGroups(
        (Guid GroupA, Guid GroupB) groups,
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
            OperationalGroupShippingGroupIds: [groups.GroupA, groups.GroupB]);

    private static async Task<Fixture> CreateFixtureAsync(
        OrderDbContext orderDb,
        FulfillmentDbContext fulfillmentDb,
        bool allowManage,
        SeededLines seeded)
    {
        await EnsurePackedFulfillmentsAsync(orderDb, fulfillmentDb, seeded);
        var directory = CreateFulfillmentDirectory(fulfillmentDb);
        var adapter = new FulfillmentAdminOperationsAdapter(
            directory,
            new FulfillmentShippedQuantityReader(fulfillmentDb),
            new ShippingMethodsOptions());
        var factsReader = new OrderCommittedCarrierShipmentFactsReader(orderDb, adapter);
        var historyReader = new OrderShippingGroupHistoryReader(orderDb);
        var workflow = new MarketplaceOperationalGroupWorkflow(
            adapter,
            factsReader,
            historyReader,
            new ControlledAccessReader(allowManage));
        var facts = await factsReader.ListByCheckoutAsync(CheckoutId, CancellationToken.None);
        return new Fixture(workflow, facts);
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
            (OrderId: OrderA, LineId: seeded.LineA, Quantity: 2m),
            (OrderId: OrderB, LineId: seeded.LineB, Quantity: 3m),
        };
        foreach (var row in packed)
        {
            var unit = await directory.GetBySellerOrderAsync(row.OrderId, CancellationToken.None);
            Assert.NotNull(unit);
            await directory.MarkProcessingAsync(unit!.FulfillmentId, Actor, CancellationToken.None);
            // Real packed quantities are explicit line selections; packing the reviewed quantity also
            // advances the unit to Packed, so no extra status-only transition is needed (or allowed).
            await directory.PackSelectionsAsync(
                unit.FulfillmentId,
                Actor,
                [new FulfillmentSelectionCommand(row.LineId, row.Quantity)],
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

    private static async Task<SeededLines> SeedCommittedOrderAsync(OrderDbContext db, bool shiftGroupBSlot = false)
    {
        var lineA = OrderLine.FromCheckout(
            OrderA, LineA, Guid.NewGuid(), SellerA, 2, 50_000m, "IRR", true, Guid.NewGuid(), Guid.NewGuid(),
            "Taxable", 0.09m, 9_000m, 118_000m, null);
        var lineB = OrderLine.FromCheckout(
            OrderB, LineB, Guid.NewGuid(), SellerB, 3, 40_000m, "IRR", true, Guid.NewGuid(), Guid.NewGuid(),
            "Taxable", 0.09m, 10_800m, 130_800m, null);
        var orderA = SellerOrder.Open(
            CheckoutId, SellerA, $"TB253R2A{OrderA:N}"[..20], OrderMode.OnlinePurchase, "IRR", [lineA]);
        var orderB = SellerOrder.Open(
            CheckoutId, SellerB, $"TB253R2B{OrderB:N}"[..20], OrderMode.OnlinePurchase, "IRR", [lineB]);

        // Group membership and review selections must use the persisted order-line identity, never
        // an offer/price id: the line id is the domain-owned key Fulfillment will bind shipments to.
        var groupA = OrderShippingGroup.Create(
            Plan(GroupA, SellerA, RowA, lineA.LineId),
            CheckoutId,
            orderA.SellerOrderId,
            Now);
        groupA.AddLine(lineA.LineId);
        var groupB = OrderShippingGroup.Create(
            Plan(GroupB, SellerB, RowB, lineB.LineId, shiftGroupBSlot ? OpenTicks + 2L * TimeSpan.TicksPerHour : null),
            CheckoutId,
            orderB.SellerOrderId,
            Now);
        groupB.AddLine(lineB.LineId);

        var checkout = CheckoutGroup.Submit(
            CheckoutId,
            $"tb253r2-{CheckoutId:N}",
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
        return new SeededLines(lineA.LineId, lineB.LineId);
    }

    private sealed record SeededLines(Guid LineA, Guid LineB);

    /// <summary>
    /// One accepted Marketplace group: the marketplace operator is the committed responsibility/origin,
    /// the receiving day/hour is authoritative evidence, and the fee is the accepted payable.
    /// </summary>
    private static OrderShippingGroupPlan Plan(
        Guid groupId,
        Guid sellerPartyId,
        Guid rateRowId,
        Guid lineId,
        long? openOverride = null)
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
            null,
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
        var builder = new DbContextOptionsBuilder<OrderDbContext>();
        ToobaNpgsql.ConfigureModuleContext(builder, target.ConnectionString, OrderDbContext.Schema, typeof(OrderDbContext));
        var db = new OrderDbContext(builder.Options);
        await db.Database.MigrateAsync();
        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM \"order\".order_shipping_group_lines;"
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

    private static async Task<FulfillmentDbContext> ReadyFulfillmentAsync(OrderEvidenceRelationalTestTarget target)
    {
        var builder = new DbContextOptionsBuilder<FulfillmentDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            builder,
            target.ConnectionString,
            FulfillmentDbContext.Schema,
            typeof(FulfillmentDbContext));
        var db = new FulfillmentDbContext(builder.Options);
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

    private sealed record Fixture(
        MarketplaceOperationalGroupWorkflow Workflow,
        IReadOnlyList<OrderCommittedCarrierShipmentFacts> Facts);

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


