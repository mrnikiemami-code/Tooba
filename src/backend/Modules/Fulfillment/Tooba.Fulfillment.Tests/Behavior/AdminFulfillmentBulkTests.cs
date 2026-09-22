using Tooba.Fulfillment.Application.Commands.ExecuteAdminFulfillmentBulk;
using Tooba.Fulfillment.Application.Models;
using Tooba.Fulfillment.Application.Ports;
using Tooba.Fulfillment.Contracts.Errors;
using Tooba.Fulfillment.Domain.ValueObjects;
using Tooba.Order.Contracts.Fulfillment;
using Xunit;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>TB-TMAR-NEXT-MODULE-BATCH-004-R2 — work-queue bulk Application ownership.</summary>
public sealed class AdminFulfillmentBulkTests
{
    [Fact]
    public async Task Unsupported_action_returns_semantic_code()
    {
        var result = await Run(new StubDir([]), new StubOps(), "nope", []);
        Assert.Equal(FulfillmentErrorCodes.WorkQueueBulkUnsupported, result.ErrorCode);
    }

    [Fact]
    public async Task Empty_items_returns_empty_code()
    {
        var result = await Run(new StubDir([]), new StubOps(), "mark_processing", []);
        Assert.Equal(FulfillmentErrorCodes.WorkQueueBulkEmpty, result.ErrorCode);
    }

    [Fact]
    public async Task Cross_seller_is_rejected_before_execution()
    {
        var a = Snapshot(Guid.NewGuid(), FulfillmentStatus.ReadyToFulfill);
        var b = Snapshot(Guid.NewGuid(), FulfillmentStatus.ReadyToFulfill);
        var result = await Run(
            new StubDir([a, b]),
            new StubOps(),
            "mark_processing",
            [
                Item(a),
                Item(b),
            ]);
        Assert.Equal(FulfillmentErrorCodes.WorkQueueCrossSeller, result.ErrorCode);
        Assert.Equal(0, result.Attempted);
    }

    [Fact]
    public async Task Row_mismatch_is_rejected()
    {
        var snap = Snapshot(Guid.NewGuid(), FulfillmentStatus.ReadyToFulfill);
        var result = await Run(
            new StubDir([snap]),
            new StubOps(),
            "mark_processing",
            [new AdminFulfillmentWorkQueueBulkItem(snap.CheckoutId, snap.FulfillmentId, Guid.NewGuid(), null)]);
        Assert.Equal(FulfillmentErrorCodes.WorkQueueRowMismatch, result.ErrorCode);
    }

    [Fact]
    public async Task Incompatible_action_is_rejected()
    {
        // ReadyToFulfill projects mark_processing only — not mark_packed.
        var snap = Snapshot(Guid.NewGuid(), FulfillmentStatus.ReadyToFulfill);
        var result = await Run(new StubDir([snap]), new StubOps(), "mark_packed", [Item(snap)]);
        Assert.Equal(FulfillmentErrorCodes.WorkQueueIncompatible, result.ErrorCode);
    }

    [Fact]
    public async Task Dispatch_without_projectable_shipment_is_incompatible_not_executed()
    {
        var lineId = Guid.NewGuid();
        var snap = Snapshot(
            Guid.NewGuid(),
            FulfillmentStatus.Packed,
            [new FulfillmentItemSnapshot(Guid.NewGuid(), lineId, 1, 0, null, 1, 1)],
            [
                new ShipmentSnapshot(
                    Guid.NewGuid(),
                    ShipmentStatus.Created,
                    "پست",
                    null,
                    null,
                    null,
                    [new ShipmentLineSnapshot(lineId, 1)],
                    default),
            ]);
        var result = await Run(new StubDir([snap]), new StubOps(), "dispatch_shipment", [Item(snap)]);
        Assert.Equal(FulfillmentErrorCodes.WorkQueueIncompatible, result.ErrorCode);
        Assert.Equal(0, result.Attempted);
    }

    [Fact]
    public async Task Deliver_without_dispatched_shipment_is_incompatible()
    {
        var lineId = Guid.NewGuid();
        var snap = Snapshot(
            Guid.NewGuid(),
            FulfillmentStatus.Packed,
            [new FulfillmentItemSnapshot(Guid.NewGuid(), lineId, 1, 0, null, 1, 1)],
            [
                new ShipmentSnapshot(
                    Guid.NewGuid(),
                    ShipmentStatus.Created,
                    "پست",
                    "TRK",
                    null,
                    null,
                    [new ShipmentLineSnapshot(lineId, 1)],
                    default),
            ]);
        var result = await Run(new StubDir([snap]), new StubOps(), "deliver_shipment", [Item(snap)]);
        Assert.Equal(FulfillmentErrorCodes.WorkQueueIncompatible, result.ErrorCode);
    }

    [Fact]
    public async Task Partial_success_reports_attempted_and_succeeded_after_downstream_failure()
    {
        var seller = Guid.NewGuid();
        var a = Snapshot(seller, FulfillmentStatus.ReadyToFulfill);
        var b = Snapshot(seller, FulfillmentStatus.ReadyToFulfill);
        var result = await Run(
            new StubDir([a, b]),
            new StubOps(failOnCall: 2),
            "mark_processing",
            [Item(a), Item(b)]);
        Assert.Equal(2, result.Attempted);
        Assert.Equal(1, result.Succeeded);
        Assert.Equal("order.operation.failed", result.ErrorCode);
    }

    [Fact]
    public async Task Successful_bulk_clears_error_code()
    {
        var a = Snapshot(Guid.NewGuid(), FulfillmentStatus.ReadyToFulfill);
        var result = await Run(new StubDir([a]), new StubOps(), "mark_processing", [Item(a)]);
        Assert.Null(result.ErrorCode);
        Assert.Equal(1, result.Attempted);
        Assert.Equal(1, result.Succeeded);
    }

    private static async Task<AdminFulfillmentWorkQueueBulkResult> Run(
        StubDir dir,
        StubOps ops,
        string action,
        IReadOnlyList<AdminFulfillmentWorkQueueBulkItem> items)
    {
        var handler = new ExecuteAdminFulfillmentBulkHandler(dir, ops);
        var outcome = await handler.Handle(
            new ExecuteAdminFulfillmentBulkCommand(
                Guid.NewGuid(),
                new AdminFulfillmentWorkQueueBulkRequest(action, items)),
            CancellationToken.None);
        Assert.True(outcome.IsSuccess);
        return outcome.Value;
    }

    private static AdminFulfillmentWorkQueueBulkItem Item(FulfillmentSnapshot snap) =>
        new(snap.CheckoutId, snap.FulfillmentId, snap.SellerOrderId, null);

    private static FulfillmentSnapshot Snapshot(
        Guid sellerPartyId,
        FulfillmentStatus status,
        IReadOnlyList<FulfillmentItemSnapshot>? items = null,
        IReadOnlyList<ShipmentSnapshot>? shipments = null)
    {
        var lineId = Guid.NewGuid();
        return new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            sellerPartyId,
            status,
            "گیرنده",
            "0912",
            "تهران",
            "تهران",
            "آدرس",
            "1234567890",
            "post",
            "پست",
            items ?? [new FulfillmentItemSnapshot(Guid.NewGuid(), lineId, 2, 0, null, 0)],
            shipments ?? [],
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);
    }

    private sealed class StubDir(IEnumerable<FulfillmentSnapshot> rows) : IFulfillmentDirectory
    {
        private readonly Dictionary<Guid, FulfillmentSnapshot> _map = rows.ToDictionary(x => x.FulfillmentId);

        public Task<FulfillmentSnapshot?> GetAsync(Guid fulfillmentId, CancellationToken cancellationToken) =>
            Task.FromResult(_map.TryGetValue(fulfillmentId, out var s) ? s : null);

        public Task<FulfillmentSnapshot?> GetBySellerOrderAsync(Guid sellerOrderId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
        public Task<IReadOnlyList<FulfillmentSnapshot>> ListForSellerAsync(Guid sellerPartyId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
        public Task<IReadOnlyList<FulfillmentSnapshot>> ListAllAsync(CancellationToken cancellationToken) =>
            throw new NotImplementedException();
        public Task<IReadOnlyList<FulfillmentSnapshot>> ListForCheckoutAsync(Guid checkoutId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
        public Task<FulfillmentSnapshot> MarkProcessingAsync(Guid fulfillmentId, Guid actorUserId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
        public Task<FulfillmentSnapshot> ProcessSelectionsAsync(Guid fulfillmentId, Guid actorUserId, IReadOnlyList<FulfillmentSelectionCommand> selections, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
        public Task<FulfillmentSnapshot> UnprocessSelectionsAsync(Guid fulfillmentId, Guid actorUserId, IReadOnlyList<FulfillmentSelectionCommand> selections, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
        public Task<FulfillmentSnapshot> MarkPackedAsync(Guid fulfillmentId, Guid actorUserId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
        public Task<FulfillmentSnapshot> PackSelectionsAsync(Guid fulfillmentId, Guid actorUserId, IReadOnlyList<FulfillmentSelectionCommand> selections, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
        public Task<FulfillmentSnapshot> UnpackSelectionsAsync(Guid fulfillmentId, Guid actorUserId, IReadOnlyList<FulfillmentSelectionCommand> selections, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
        public Task<FulfillmentSnapshot> CreateShipmentAsync(Guid fulfillmentId, Guid actorUserId, string carrierDisplayName, IReadOnlyList<ShipmentLineCommand> items, CancellationToken cancellationToken, string? shippingMethodCode = null, string? providerMetadataJson = null) =>
            throw new NotImplementedException();
        public Task<FulfillmentSnapshot> CancelShipmentAsync(Guid fulfillmentId, Guid shipmentId, Guid actorUserId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
        public Task<FulfillmentSnapshot> AssignTrackingAsync(Guid fulfillmentId, Guid shipmentId, Guid actorUserId, string trackingReference, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
        public Task<FulfillmentSnapshot> CorrectTrackingAsync(Guid fulfillmentId, Guid shipmentId, Guid actorUserId, string trackingReference, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
        public Task<FulfillmentSnapshot> DispatchShipmentAsync(Guid fulfillmentId, Guid shipmentId, Guid actorUserId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
        public Task<FulfillmentSnapshot> DeliverShipmentAsync(Guid fulfillmentId, Guid shipmentId, Guid actorUserId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
        public Task VoidUnstartedForCheckoutAsync(Guid checkoutId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
        public Task EnsureCreatedForPaidCheckoutAsync(Guid checkoutId, IReadOnlyList<Guid> sellerOrderIds, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
        public Task AbortForCheckoutCancelAsync(Guid checkoutId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
        public Task ReactivateAfterOrderRestoreAsync(Guid checkoutId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
        public Task RebindActiveReservationsFromOrderAsync(Guid checkoutId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
        public Task<IReadOnlyList<ConsolidatedPackageSnapshot>> GetPackagesForCheckoutAsync(Guid checkoutId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
        public Task<IReadOnlyList<ActivePackageMembershipSnapshot>> GetActiveMembershipByShipmentIdsAsync(IReadOnlyList<Guid> shipmentIds, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
        public Task<bool> IsShipmentLockedByPackageAsync(Guid shipmentId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
        public Task<ConsolidatedPackageSnapshot> CreateConsolidatedPackageAsync(Guid checkoutId, IReadOnlyList<Guid> shipmentIds, string? shippingMethodCode, string? trackingReference, string? note, Guid actorUserId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
        public Task<ConsolidatedPackageSnapshot> CancelConsolidatedPackageAsync(Guid consolidatedPackageId, Guid actorUserId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
        public Task<ConsolidatedPackageSnapshot> AssignConsolidatedPackageTrackingAsync(Guid consolidatedPackageId, string trackingReference, Guid actorUserId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
        public Task<ConsolidatedPackageSnapshot> DispatchConsolidatedPackageAsync(Guid consolidatedPackageId, Guid actorUserId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
        public Task<ConsolidatedPackageSnapshot> DeliverConsolidatedPackageAsync(Guid consolidatedPackageId, Guid actorUserId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
        public Task VoidActivePackagesForCheckoutCancelAsync(Guid checkoutId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }

    private sealed class StubOps(int? failOnCall = null) : IAdminOrderFulfillmentOperations
    {
        private int _calls;

        public Task<AdminOrderFulfillmentOperationOutcome> TryExecuteAsync(
            Guid checkoutId,
            Guid actorUserId,
            AdminOrderFulfillmentOperationRequest request,
            CancellationToken cancellationToken)
        {
            _calls++;
            if (failOnCall is int n && _calls == n)
            {
                return Task.FromResult(new AdminOrderFulfillmentOperationOutcome(false, "order.operation.failed"));
            }

            return Task.FromResult(new AdminOrderFulfillmentOperationOutcome(true, null));
        }
    }
}

