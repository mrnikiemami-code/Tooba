using Tooba.Order.Application.Admin.Operations.Policies;
using Tooba.Fulfillment.Application.Models;
using Tooba.Fulfillment.Application.Ports;
using Tooba.Fulfillment.Application.Shipping;
using Tooba.Fulfillment.Domain.ValueObjects;
using Tooba.Order.Contracts.Fulfillment;
using Tooba.Order.Infrastructure.Admin.Fulfillment;
using Xunit;

using Tooba.Order.Application.Checkout.Abuse;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Checkout.Policies;
using Tooba.Order.Application.Checkout.Process;
using Tooba.Order.Application.PurchaseVerification;
using Tooba.Order.Application.ReservationCycle.Contracts;
using Tooba.Order.Application.ReservationCycle.Policies;
using Tooba.Order.Application.ReservationCycle.Services;
using Tooba.Order.Application.Seller.Policies;

namespace Tooba.Host.Tests;

/// <summary>TB-TMAR-NEXT-MODULE-BATCH-004-R4 — Order-owned adapter anti-pattern closure.</summary>
public sealed class AdminOrderFulfillmentOperationsTests
{
    [Fact]
    public async Task Mark_processing_succeeds_through_order_owned_adapter()
    {
        var fulfillmentId = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var checkoutId = Guid.NewGuid();
        var dir = new StubDirectory(fulfillmentId, sellerOrderId, checkoutId);
        var ops = new AdminOrderFulfillmentOperations(
            new StubCheckout(false, [sellerOrderId]),
            new StubPermissions(true),
            dir);

        var outcome = await ops.TryExecuteAsync(
            checkoutId,
            Guid.NewGuid(),
            new AdminOrderFulfillmentOperationRequest(
                "mark_processing",
                sellerOrderId,
                fulfillmentId,
                null,
                null,
                null,
                null),
            CancellationToken.None);

        Assert.True(outcome.Succeeded);
        Assert.Null(outcome.ErrorCode);
        Assert.Equal(1, dir.MarkProcessingCalls);
    }

    [Fact]
    public async Task Denied_permission_preserves_stable_code()
    {
        var fulfillmentId = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var checkoutId = Guid.NewGuid();
        var ops = new AdminOrderFulfillmentOperations(
            new StubCheckout(false, [sellerOrderId]),
            new StubPermissions(false),
            new StubDirectory(fulfillmentId, sellerOrderId, checkoutId));

        var outcome = await ops.TryExecuteAsync(
            checkoutId,
            Guid.NewGuid(),
            Req("mark_processing", sellerOrderId, fulfillmentId, null),
            CancellationToken.None);

        Assert.False(outcome.Succeeded);
        Assert.Equal("order.operation.denied", outcome.ErrorCode);
    }

    [Fact]
    public async Task Cancelled_checkout_preserves_stable_code()
    {
        var fulfillmentId = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var checkoutId = Guid.NewGuid();
        var ops = new AdminOrderFulfillmentOperations(
            new StubCheckout(true, [sellerOrderId]),
            new StubPermissions(true),
            new StubDirectory(fulfillmentId, sellerOrderId, checkoutId));

        var outcome = await ops.TryExecuteAsync(
            checkoutId,
            Guid.NewGuid(),
            Req("mark_processing", sellerOrderId, fulfillmentId, null),
            CancellationToken.None);

        Assert.False(outcome.Succeeded);
        Assert.Equal("order.cancelled.blocks_action", outcome.ErrorCode);
    }

    [Fact]
    public async Task Downstream_stable_fulfillment_code_preserves_mapping()
    {
        var fulfillmentId = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var checkoutId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var dir = new StubDirectory(fulfillmentId, sellerOrderId, checkoutId)
        {
            DispatchThrows = new InvalidOperationException("fulfillment.dispatch.tracking_required"),
        };
        var ops = new AdminOrderFulfillmentOperations(
            new StubCheckout(false, [sellerOrderId]),
            new StubPermissions(true),
            dir);

        var outcome = await ops.TryExecuteAsync(
            checkoutId,
            Guid.NewGuid(),
            Req("dispatch_shipment", sellerOrderId, fulfillmentId, shipmentId),
            CancellationToken.None);

        Assert.False(outcome.Succeeded);
        Assert.Equal("fulfillment.dispatch.tracking_required", outcome.ErrorCode);
    }

    [Fact]
    public async Task Unexpected_exception_is_not_silently_converted_to_generic_failure()
    {
        var fulfillmentId = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var checkoutId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        var dir = new StubDirectory(fulfillmentId, sellerOrderId, checkoutId)
        {
            DispatchThrows = new InvalidOperationException("dispatch بدون tracking مجاز نیست."),
        };
        var ops = new AdminOrderFulfillmentOperations(
            new StubCheckout(false, [sellerOrderId]),
            new StubPermissions(true),
            dir);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ops.TryExecuteAsync(
                checkoutId,
                Guid.NewGuid(),
                Req("dispatch_shipment", sellerOrderId, fulfillmentId, shipmentId),
                CancellationToken.None));
        Assert.Equal("dispatch بدون tracking مجاز نیست.", ex.Message);
    }

    [Fact]
    public void Implementation_lives_in_order_infrastructure_without_antipatterns()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        Assert.False(File.Exists(Path.Combine(root, "Host", "Tooba.Host", "Admin", "HostAdminOrderFulfillmentOperations.cs")));
        var impl = Path.Combine(root, "Modules", "Order", "Tooba.Order.Infrastructure", "Admin", "Fulfillment", "AdminOrderFulfillmentOperations.cs");
        Assert.True(File.Exists(impl));
        var text = File.ReadAllText(impl);
        Assert.Contains("namespace Tooba.Order.Infrastructure.Admin.Fulfillment", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host", text, StringComparison.Ordinal);
        Assert.DoesNotContain("AdminOrderOperationsPolicy", text, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", text, StringComparison.Ordinal);
        Assert.DoesNotContain("MapKnownOperationException", text, StringComparison.Ordinal);
        Assert.DoesNotMatch(new System.Text.RegularExpressions.Regex(@"[\u0600-\u06FF]"), text);
    }

    private static AdminOrderFulfillmentOperationRequest Req(
        string code,
        Guid sellerOrderId,
        Guid fulfillmentId,
        Guid? shipmentId) =>
        new(code, sellerOrderId, fulfillmentId, shipmentId, null, null, null);

    private sealed class StubCheckout(bool cancelled, IReadOnlyList<Guid> sellerOrderIds) : IAdminOrderFulfillmentCheckoutReader
    {
        public Task<AdminOrderFulfillmentCheckoutSnapshot?> GetAsync(Guid checkoutId, CancellationToken cancellationToken) =>
            Task.FromResult<AdminOrderFulfillmentCheckoutSnapshot?>(
                new AdminOrderFulfillmentCheckoutSnapshot(cancelled, sellerOrderIds));
    }

    private sealed class StubPermissions(bool allowed) : IAdminOrderFulfillmentPermissionGate
    {
        public Task<bool> CanManageFulfillmentAsync(Guid actorUserId, CancellationToken cancellationToken) =>
            Task.FromResult(allowed);
    }

    private sealed class StubDirectory : IFulfillmentDirectory
    {
        private readonly Guid _fulfillmentId;
        private readonly Guid _sellerOrderId;
        private readonly Guid _checkoutId;

        public StubDirectory(Guid fulfillmentId, Guid sellerOrderId, Guid checkoutId)
        {
            _fulfillmentId = fulfillmentId;
            _sellerOrderId = sellerOrderId;
            _checkoutId = checkoutId;
        }

        public int MarkProcessingCalls { get; private set; }
        public Exception? DispatchThrows { get; init; }

        public Task<FulfillmentSnapshot?> GetAsync(Guid fulfillmentId, CancellationToken cancellationToken) =>
            Task.FromResult<FulfillmentSnapshot?>(Snapshot());

        public Task<FulfillmentSnapshot> MarkProcessingAsync(Guid fulfillmentId, Guid actorUserId, CancellationToken cancellationToken)
        {
            MarkProcessingCalls++;
            return Task.FromResult(Snapshot());
        }

        public Task<FulfillmentSnapshot> DispatchShipmentAsync(Guid fulfillmentId, Guid shipmentId, Guid actorUserId, CancellationToken cancellationToken)
        {
            if (DispatchThrows is not null)
            {
                throw DispatchThrows;
            }

            return Task.FromResult(Snapshot());
        }

        private FulfillmentSnapshot Snapshot() =>
            new(
                _fulfillmentId,
                _sellerOrderId,
                _checkoutId,
                Guid.NewGuid(),
                FulfillmentStatus.Processing,
                "r",
                "09",
                "p",
                "c",
                "a",
                "1",
                "post",
                "پست",
                [new FulfillmentItemSnapshot(Guid.NewGuid(), Guid.NewGuid(), 2m, 0m, null, 0m, 1m)],
                []);

        public Task<FulfillmentSnapshot?> GetBySellerOrderAsync(Guid sellerOrderId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
        public Task<IReadOnlyList<FulfillmentSnapshot>> ListForSellerAsync(Guid sellerPartyId, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
        public Task<IReadOnlyList<FulfillmentSnapshot>> ListAllAsync(CancellationToken cancellationToken) =>
            throw new NotImplementedException();
        public Task<IReadOnlyList<FulfillmentSnapshot>> ListForCheckoutAsync(Guid checkoutId, CancellationToken cancellationToken) =>
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
}
