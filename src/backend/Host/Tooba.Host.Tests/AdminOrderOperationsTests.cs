using Tooba.Fulfillment.Contracts.Operations;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Policies;
using Tooba.Order.Application.Admin.Operations.Ports;
using Tooba.Order.Application.Admin.Operations.Services;
using Tooba.Order.Domain;
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

/// <summary>
/// تست‌های متمرکز projection عملیات سفارش ادمین (Order Application policy ownership).
/// </summary>
public sealed class AdminOrderOperationsTests
{
    [Fact]
    public void CanCancel_for_pending_payment()
    {
        var order = CreateSellerOrder(paid: false);
        Assert.Equal(SellerOrderStatus.PendingPayment, order.Status);
        Assert.True(AdminOrderOperationsPolicy.CanCancel(order, fulfillment: null));
    }

    [Fact]
    public void CanCancel_false_when_fulfillment_delivered()
    {
        var order = CreateSellerOrder(paid: true);
        var fulfillment = new FulfillmentSnapshot(
            Guid.NewGuid(),
            order.SellerOrderId,
            Guid.NewGuid(),
            order.SellerPartyId,
            FulfillmentOperationStatus.Delivered,
            "n",
            "m",
            "p",
            "c",
            "a",
            "1",
            "post",
            "پست",
            [],
            [
                new ShipmentSnapshot(
                    Guid.NewGuid(),
                    ShipmentOperationStatus.Delivered,
                    "Post",
                    "TRK",
                    DateTimeOffset.UtcNow.AddDays(-2),
                    DateTimeOffset.UtcNow.AddDays(-1),
                    []),
            ]);
        Assert.False(AdminOrderOperationsPolicy.CanCancel(order, fulfillment));
    }

    [Fact]
    public void CanCancel_paid_when_packed_or_created_shipment()
    {
        var order = CreateSellerOrder(paid: true);
        var packed = new FulfillmentSnapshot(
            Guid.NewGuid(),
            order.SellerOrderId,
            Guid.NewGuid(),
            order.SellerPartyId,
            FulfillmentOperationStatus.Packed,
            "n",
            "m",
            "p",
            "c",
            "a",
            "1",
            "post",
            "پست",
            [new FulfillmentItemSnapshot(Guid.NewGuid(), Guid.NewGuid(), 2, 0, Guid.NewGuid(), 2, 2)],
            [
                new ShipmentSnapshot(
                    Guid.NewGuid(),
                    ShipmentOperationStatus.Created,
                    "Post",
                    "TRK",
                    null,
                    null,
                    []),
            ]);
        Assert.True(AdminOrderOperationsPolicy.CanCancel(order, packed));
        Assert.True(AdminOrderOperationsPolicy.CanCancel(order, fulfillment: null));
    }

    [Fact]
    public void CanCancel_paid_when_created_shipment_has_tracking()
    {
        var order = CreateSellerOrder(paid: true);
        var createdTracked = new FulfillmentSnapshot(
            Guid.NewGuid(),
            order.SellerOrderId,
            Guid.NewGuid(),
            order.SellerPartyId,
            FulfillmentOperationStatus.Packed,
            "n",
            "m",
            "p",
            "c",
            "a",
            "1",
            "post",
            "پست",
            [new FulfillmentItemSnapshot(Guid.NewGuid(), Guid.NewGuid(), 1, 0, Guid.NewGuid(), 1, 1)],
            [
                new ShipmentSnapshot(
                    Guid.NewGuid(),
                    ShipmentOperationStatus.Created,
                    "Post",
                    "TRK-1",
                    null,
                    null,
                    []),
            ]);
        Assert.True(AdminOrderOperationsPolicy.CanCancel(order, createdTracked));
        Assert.False(AdminOrderOperationsPolicy.HasDispatchedQuantity(createdTracked));
    }

    [Fact]
    public void HasDispatchedQuantity_true_for_unit_dispatched_status()
    {
        var order = CreateSellerOrder(paid: true);
        var unitOnly = new FulfillmentSnapshot(
            Guid.NewGuid(),
            order.SellerOrderId,
            Guid.NewGuid(),
            order.SellerPartyId,
            FulfillmentOperationStatus.Dispatched,
            "n",
            "m",
            "p",
            "c",
            "a",
            "1",
            "post",
            "پست",
            [new FulfillmentItemSnapshot(Guid.NewGuid(), Guid.NewGuid(), 1, 1, Guid.NewGuid(), 1, 1)],
            []);
        Assert.True(AdminOrderOperationsPolicy.HasDispatchedQuantity(unitOnly));
    }

    [Fact]
    public void HasDispatchedOrDelivered_ignores_created_only()
    {
        var order = CreateSellerOrder(paid: true);
        var created = new FulfillmentSnapshot(
            Guid.NewGuid(),
            order.SellerOrderId,
            Guid.NewGuid(),
            order.SellerPartyId,
            FulfillmentOperationStatus.Packed,
            "n",
            "m",
            "p",
            "c",
            "a",
            "1",
            "post",
            "پست",
            [],
            [
                new ShipmentSnapshot(
                    Guid.NewGuid(),
                    ShipmentOperationStatus.Created,
                    "Post",
                    null,
                    null,
                    null,
                    []),
            ]);
        var dispatched = created with
        {
            Shipments =
            [
                new ShipmentSnapshot(
                    Guid.NewGuid(),
                    ShipmentOperationStatus.Dispatched,
                    "Post",
                    "TRK",
                    DateTimeOffset.UtcNow,
                    null,
                    []),
            ],
        };
        Assert.False(AdminOrderOperationsPolicy.HasDispatchedOrDelivered([created]));
        Assert.True(AdminOrderOperationsPolicy.HasDispatchedOrDelivered([created, dispatched]));
    }

    [Fact]
    public void Has_allows_legacy_admin_without_ops_family_grants()
    {
        var effective = new OrderAdminEffectiveAccess([]);
        Assert.True(AdminOrderOperationsPolicy.Has(effective, "order.cancel"));
        Assert.True(AdminOrderOperationsPolicy.Has(effective, "return.manage"));
    }

    [Fact]
    public void Has_requires_specific_perm_when_ops_family_present()
    {
        var effective = new OrderAdminEffectiveAccess(
            [new OrderAdminPermissionGrant("order.view", DeniedByCeiling: false)]);
        Assert.False(AdminOrderOperationsPolicy.Has(effective, "order.cancel"));
        Assert.True(AdminOrderOperationsPolicy.Has(
            new OrderAdminEffectiveAccess(
                [new OrderAdminPermissionGrant("order.cancel", DeniedByCeiling: false)]),
            "order.cancel"));
    }

    [Fact]
    public void Endpoints_register_operations_routes_and_stable_error_codes()
    {
        var root = FindRepoRoot();
        var endpoints = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Modules", "Order", "Tooba.Order.Endpoints", "Admin", "Operations", "AdminOrderOperationsEndpoints.cs"));
        var composer = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Modules", "Order", "Tooba.Order.Application", "Admin", "Operations", "Services", "AdminOrderOperationsOrchestrator.cs"));
        var program = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Host", "Tooba.Host", "Program.cs"));
        Assert.Contains("/{checkoutId:guid}/operations", endpoints, StringComparison.Ordinal);
        Assert.Contains("return-eligibility", endpoints, StringComparison.Ordinal);
        Assert.Contains("ISender", endpoints, StringComparison.Ordinal);
        Assert.Contains("order.operation.denied", composer, StringComparison.Ordinal);
        Assert.Contains("order.operation.invalid", composer, StringComparison.Ordinal);
        Assert.Contains("ContractOperationException", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("MapKnownOperationException", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("MapAdminOrderInventoryRecoverySupplyEndpoints", program, StringComparison.Ordinal);
        Assert.Contains("MapOrderEndpoints", program, StringComparison.Ordinal);
        Assert.DoesNotContain("AdminOrderOperationsComposer", program, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(root, "src", "backend", "Modules", "Order", "Tooba.Order.Application", "Admin", "Operations", "Services", "AdminOrderOperationsOrchestrator.cs")));
        Assert.True(File.Exists(Path.Combine(root, "src", "backend", "Modules", "Order", "Tooba.Order.Endpoints", "Admin", "Operations", "AdminOrderOperationsEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(root, "src", "backend", "Host", "Tooba.Host", "Admin", "AdminOrderOperationsModels.cs")));
        Assert.False(File.Exists(Path.Combine(root, "src", "backend", "Host", "Tooba.Host", "Admin", "AdminOrderOperationsComposer.cs")));
        Assert.False(File.Exists(Path.Combine(root, "src", "backend", "Host", "Tooba.Host", "Admin", "AdminOrderOperationsEndpoints.cs")));
    }

    private static AdminOrderOpsSellerOrderSnapshot CreateSellerOrder(bool paid)
    {
        var status = paid ? SellerOrderStatus.Paid : SellerOrderStatus.PendingPayment;
        return new AdminOrderOpsSellerOrderSnapshot(
            Guid.NewGuid(),
            Guid.NewGuid(),
            status,
            null,
            [new AdminOrderOpsLineSnapshot(Guid.NewGuid(), 1)]);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
