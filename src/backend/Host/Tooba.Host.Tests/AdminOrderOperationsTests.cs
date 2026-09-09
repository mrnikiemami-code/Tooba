using Tooba.AccessControl.Application;
using Tooba.AccessControl.Domain;
using Tooba.Fulfillment.Application;
using Tooba.Fulfillment.Domain;
using Tooba.Host.Admin;
using Tooba.Order.Domain;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// تست‌های متمرکز projection عملیات سفارش ادمین.
/// </summary>
public sealed class AdminOrderOperationsTests
{
    [Fact]
    public void CanCancel_for_pending_payment()
    {
        var order = CreateSellerOrder(paid: false);
        Assert.Equal(SellerOrderStatus.PendingPayment, order.Status);
        Assert.True(AdminOrderOperationsComposer.CanCancel(order, fulfillment: null));
    }

    [Fact]
    public void CanCancel_false_when_fulfillment_delivered()
    {
        var order = CreateSellerOrder(paid: true);
        var fulfillment = new FulfillmentSnapshot(
            Guid.NewGuid(),
            order.SellerOrderId,
            order.CheckoutId,
            order.SellerPartyId,
            FulfillmentStatus.Delivered,
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
                    ShipmentStatus.Delivered,
                    "Post",
                    "TRK",
                    DateTimeOffset.UtcNow.AddDays(-2),
                    DateTimeOffset.UtcNow.AddDays(-1),
                    []),
            ]);
        Assert.False(AdminOrderOperationsComposer.CanCancel(order, fulfillment));
    }

    [Fact]
    public void CanCancel_paid_when_packed_or_created_shipment()
    {
        var order = CreateSellerOrder(paid: true);
        var packed = new FulfillmentSnapshot(
            Guid.NewGuid(),
            order.SellerOrderId,
            order.CheckoutId,
            order.SellerPartyId,
            FulfillmentStatus.Packed,
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
                    ShipmentStatus.Created,
                    "Post",
                    "TRK",
                    null,
                    null,
                    []),
            ]);
        Assert.True(AdminOrderOperationsComposer.CanCancel(order, packed));
        Assert.True(AdminOrderOperationsComposer.CanCancel(order, fulfillment: null));
    }

    [Fact]
    public void HasDispatchedOrDelivered_true_if_any_seller_has_shipped_quantity()
    {
        var created = new FulfillmentSnapshot(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            FulfillmentStatus.Packed,
            "n",
            "m",
            "p",
            "c",
            "a",
            "1",
            "post",
            "پست",
            [new FulfillmentItemSnapshot(Guid.NewGuid(), Guid.NewGuid(), 1, 0, null, 1, 1)],
            [
                new ShipmentSnapshot(
                    Guid.NewGuid(),
                    ShipmentStatus.Created,
                    "Post",
                    "TRK",
                    null,
                    null,
                    []),
            ]);
        var dispatched = new FulfillmentSnapshot(
            Guid.NewGuid(),
            Guid.NewGuid(),
            created.CheckoutId,
            Guid.NewGuid(),
            FulfillmentStatus.Dispatched,
            "n",
            "m",
            "p",
            "c",
            "a",
            "1",
            "post",
            "پست",
            [new FulfillmentItemSnapshot(Guid.NewGuid(), Guid.NewGuid(), 1, 1, null, 1, 1)],
            [
                new ShipmentSnapshot(
                    Guid.NewGuid(),
                    ShipmentStatus.Dispatched,
                    "Post",
                    "TRK",
                    DateTimeOffset.UtcNow,
                    null,
                    []),
            ]);
        Assert.False(AdminOrderOperationsComposer.HasDispatchedOrDelivered([created]));
        Assert.True(AdminOrderOperationsComposer.HasDispatchedOrDelivered([created, dispatched]));
    }

    [Fact]
    public void Has_allows_legacy_admin_without_ops_family_grants()
    {
        var effective = new EffectiveAccessDto(
            Guid.NewGuid(),
            AccessOwnerScopeKind.Platform,
            null,
            [],
            ["admin"]);
        Assert.True(AdminOrderOperationsComposer.Has(effective, "order.cancel"));
        Assert.True(AdminOrderOperationsComposer.Has(effective, "return.manage"));
    }

    [Fact]
    public void Has_requires_specific_perm_when_ops_family_present()
    {
        var effective = new EffectiveAccessDto(
            Guid.NewGuid(),
            AccessOwnerScopeKind.Platform,
            null,
            [
                new EffectivePermissionDto(
                    "order.view",
                    "Order",
                    AccessScopeKind.GlobalWithinOwner,
                    null,
                    ["ops"],
                    DeniedByCeiling: false),
            ],
            ["ops"]);
        Assert.False(AdminOrderOperationsComposer.Has(effective, "order.cancel"));
        Assert.True(AdminOrderOperationsComposer.Has(
            effective with
            {
                Permissions =
                [
                    new EffectivePermissionDto(
                        "order.cancel",
                        "Order",
                        AccessScopeKind.GlobalWithinOwner,
                        null,
                        ["ops"],
                        DeniedByCeiling: false),
                ],
            },
            "order.cancel"));
    }

    [Fact]
    public void Endpoints_register_operations_routes_and_stable_error_codes()
    {
        var root = FindRepoRoot();
        var endpoints = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Host", "Tooba.Host", "Admin", "AdminOrderOperationsEndpoints.cs"));
        var composer = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Host", "Tooba.Host", "Admin", "AdminOrderOperationsComposer.cs"));
        var program = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Host", "Tooba.Host", "Program.cs"));
        Assert.Contains("/{checkoutId:guid}/operations", endpoints, StringComparison.Ordinal);
        Assert.Contains("return-eligibility", endpoints, StringComparison.Ordinal);
        Assert.Contains("order.operation.denied", composer, StringComparison.Ordinal);
        Assert.Contains("order.operation.invalid", composer, StringComparison.Ordinal);
        Assert.Contains("order.operation.failed", composer, StringComparison.Ordinal);
        Assert.Contains("MapAdminOrderOperationsEndpoints", program, StringComparison.Ordinal);
        Assert.Contains("AdminOrderOperationsComposer", program, StringComparison.Ordinal);
    }

    private static SellerOrder CreateSellerOrder(bool paid)
    {
        var checkoutId = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var line = OrderLine.FromCheckout(
            sellerOrderId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            1000m,
            "IRR",
            true,
            Guid.NewGuid(),
            null,
            "Taxable",
            0.09m,
            90m,
            1090m,
            null);
        var order = SellerOrder.Open(
            checkoutId,
            Guid.NewGuid(),
            $"SO-{sellerOrderId:N}"[..20],
            OrderMode.OnlinePurchase,
            "IRR",
            [line]);
        if (paid)
        {
            order.RecordVerifiedPayment();
        }

        return order;
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
