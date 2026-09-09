using Tooba.Fulfillment.Application;
using Tooba.Fulfillment.Domain;
using Tooba.Host.Admin;
using Tooba.Offer.Domain;
using Tooba.Order.Domain;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-P09-T012: capability projection for selection, payment lock, shipment eligibility.</summary>
public sealed class AdminFulfillmentCapabilityTests
{
    [Fact]
    public void Waiting_payment_has_no_fulfillment_capability()
    {
        var group = SeedCheckout(cancel: false);
        var order = group.SellerOrders.Single();
        var projected = AdminFulfillmentCapabilityProjector.Project(order, fulfillment: null, []);
        Assert.True(projected.Seller.PaymentLocked);
        Assert.False(projected.Seller.SelectionAllowed);
        Assert.False(projected.Seller.ShipmentCreationPossible);
        Assert.Contains("پرداخت این بخش از سفارش هنوز تأیید نشده", projected.Seller.InfoMessageFa);
        Assert.All(projected.Lines, line => Assert.False(line.Selectable));
    }

    [Fact]
    public void ReadyToProcess_exposes_start_processing_only()
    {
        var group = SeedCheckout(cancel: false);
        var order = group.SellerOrders.Single();
        order.RecordVerifiedPayment();
        var lineId = order.Lines.Single().LineId;
        var fulfillment = Snapshot(
            order.SellerOrderId,
            FulfillmentStatus.ReadyToFulfill,
            [new FulfillmentItemSnapshot(Guid.NewGuid(), lineId, 2, 0, null, 0)]);
        var actions = new[]
        {
            new AdminOrderOperationAction("mark_processing", "شروع", "s", order.SellerOrderId, fulfillment.FulfillmentId, null, null, "order.handle", true, null),
            new AdminOrderOperationAction("pack_selected", "pack", "p", order.SellerOrderId, fulfillment.FulfillmentId, null, null, "order.handle", true, null),
        };
        var projected = AdminFulfillmentCapabilityProjector.Project(order, fulfillment, actions);
        var line = Assert.Single(projected.Lines);
        Assert.True(line.Selectable);
        Assert.Contains("mark_processing", line.RowActionCodes);
        Assert.DoesNotContain("pack_selected", line.RowActionCodes);
        Assert.Equal(0, line.ShipmentEligibleQuantity);
    }

    [Fact]
    public void Processing_one_line_keeps_siblings_startable()
    {
        var group = SeedCheckout(cancel: false, extraLine: true);
        var order = group.SellerOrders.Single();
        order.RecordVerifiedPayment();
        var first = order.Lines[0];
        var second = order.Lines[1];
        var fulfillment = Snapshot(
            order.SellerOrderId,
            FulfillmentStatus.Processing,
            [
                new FulfillmentItemSnapshot(Guid.NewGuid(), first.LineId, first.Quantity, 0, null, 0, first.Quantity),
                new FulfillmentItemSnapshot(Guid.NewGuid(), second.LineId, second.Quantity, 0, null, 0, 0),
            ]);
        var actions = new[]
        {
            new AdminOrderOperationAction("mark_processing", "شروع", "s", order.SellerOrderId, fulfillment.FulfillmentId, null, null, "order.handle", true, null),
            new AdminOrderOperationAction("pack_selected", "pack", "p", order.SellerOrderId, fulfillment.FulfillmentId, null, null, "order.handle", true, null),
            new AdminOrderOperationAction("unprocess", "برگشت", "u", order.SellerOrderId, fulfillment.FulfillmentId, null, null, "order.handle", true, null),
        };
        var projected = AdminFulfillmentCapabilityProjector.Project(order, fulfillment, actions);
        var processed = projected.Lines.Single(x => x.OrderLineId == first.LineId);
        var sibling = projected.Lines.Single(x => x.OrderLineId == second.LineId);
        Assert.Contains("pack_selected", processed.RowActionCodes);
        Assert.Contains("unprocess", processed.RowActionCodes);
        Assert.DoesNotContain("mark_processing", processed.RowActionCodes);
        Assert.Contains("mark_processing", sibling.RowActionCodes);
        Assert.DoesNotContain("pack_selected", sibling.RowActionCodes);
        Assert.DoesNotContain("unprocess", sibling.RowActionCodes);
    }

    [Fact]
    public void Composer_source_projects_capabilities_without_n_plus_one()
    {
        var root = FindRepoRoot();
        var composer = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Host", "Tooba.Host", "Admin", "AdminOrderOperationsComposer.cs"));
        var projector = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Host", "Tooba.Host", "Admin", "AdminFulfillmentCapabilityProjector.cs"));
        Assert.Contains("AdminFulfillmentCapabilityProjector.Project", composer, StringComparison.Ordinal);
        Assert.Contains("lineCaps", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("ListForCheckoutAsync(", projector, StringComparison.Ordinal);
        Assert.Contains("شروع پردازش این قلم", composer, StringComparison.Ordinal);
        Assert.Contains("برگشت از پردازش این قلم", composer, StringComparison.Ordinal);
    }

    private static CheckoutGroup SeedCheckout(bool cancel, bool extraLine = false)
    {
        var checkoutId = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var line = OrderLine.FromCheckout(
            sellerOrderId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            2,
            1000m,
            "IRR",
            true,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Taxable",
            0.09m,
            90m,
            1090m,
            null);
        var extra = extraLine
            ? OrderLine.FromCheckout(
                sellerOrderId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                2,
                1000m,
                "IRR",
                true,
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Taxable",
                0.09m,
                90m,
                1090m,
                null)
            : null;
        var order = SellerOrder.Open(
            checkoutId,
            Guid.NewGuid(),
            $"SO-{sellerOrderId:N}"[..20],
            OrderMode.OnlinePurchase,
            "IRR",
            extra is null ? [line] : [line, extra]);
        if (cancel) order.Cancel();
        return CheckoutGroup.Submit(
            checkoutId,
            $"idem-{checkoutId:N}",
            Guid.NewGuid(),
            OrderMode.OnlinePurchase,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "IR",
            "IRR",
            SalesChannel.Marketplace,
            [order],
            DateTimeOffset.UtcNow,
            "گیرنده تست",
            "09120000000",
            "تهران",
            "تهران",
            "آدرس تست",
            "1234567890",
            "post",
            "پست");
    }

    private static FulfillmentSnapshot Snapshot(
        Guid sellerOrderId,
        FulfillmentStatus status,
        IReadOnlyList<FulfillmentItemSnapshot> items) =>
        new(
            Guid.NewGuid(),
            sellerOrderId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            status,
            "n",
            "m",
            "p",
            "c",
            "a",
            "1",
            "post",
            "پست",
            items,
            []);

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
