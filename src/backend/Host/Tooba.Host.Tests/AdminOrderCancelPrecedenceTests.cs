using Tooba.Host.Admin;
using Tooba.Offer.Domain;
using Tooba.Order.Domain;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-P09-T011: cancelled checkout suppresses payment/fulfillment forward actions.</summary>
public sealed class AdminOrderCancelPrecedenceTests
{
    [Fact]
    public void IsCheckoutCancelled_true_when_every_seller_is_cancelled()
    {
        var group = SeedCheckout(cancel: true);
        Assert.True(AdminOrderOperationsComposer.IsCheckoutCancelled(group));
    }

    [Fact]
    public void IsCheckoutCancelled_false_when_pending_payment()
    {
        var group = SeedCheckout(cancel: false);
        Assert.False(AdminOrderOperationsComposer.IsCheckoutCancelled(group));
    }

    [Fact]
    public void Cancelled_blocked_codes_cover_payment_and_fulfillment_forward_ops()
    {
        Assert.Contains("confirm_deposit", AdminOrderOperationsComposer.CancelledBlockedCodes);
        Assert.Contains("reject_deposit", AdminOrderOperationsComposer.CancelledBlockedCodes);
        Assert.Contains("restore_deposit", AdminOrderOperationsComposer.CancelledBlockedCodes);
        Assert.Contains("unconfirm_deposit", AdminOrderOperationsComposer.CancelledBlockedCodes);
        Assert.Contains("mark_processing", AdminOrderOperationsComposer.CancelledBlockedCodes);
        Assert.Contains("pack_selected", AdminOrderOperationsComposer.CancelledBlockedCodes);
        Assert.Contains("unprocess", AdminOrderOperationsComposer.CancelledBlockedCodes);
        Assert.Contains("unpack", AdminOrderOperationsComposer.CancelledBlockedCodes);
        Assert.Contains("create_shipment", AdminOrderOperationsComposer.CancelledBlockedCodes);
        Assert.Contains("dispatch_shipment", AdminOrderOperationsComposer.CancelledBlockedCodes);
        Assert.DoesNotContain("restore_cancelled_order", AdminOrderOperationsComposer.CancelledBlockedCodes);
        Assert.DoesNotContain("cancel", AdminOrderOperationsComposer.CancelledBlockedCodes);
    }

    [Fact]
    public void Cancelled_block_maps_to_human_fa()
    {
        Assert.Equal(
            "سفارش لغوشده است؛ این عملیات مجاز نیست.",
            AdminOrderOperationsComposer.FulfillmentOpToFa("order.cancelled.blocks_action"));
    }

    [Fact]
    public void Composer_source_skips_payment_and_fulfillment_when_cancelled()
    {
        var root = FindRepoRoot();
        var composer = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Host", "Tooba.Host", "Admin", "AdminOrderOperationsComposer.cs"));
        var panel = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Host", "Tooba.Host", "Admin", "AdminPanelComposer.cs"));
        Assert.Contains("if (!IsCheckoutCancelled(group))", composer, StringComparison.Ordinal);
        Assert.Contains("ProjectPaymentActions", composer, StringComparison.Ordinal);
        Assert.Contains("CancelledBlockedCodes.Contains(code)", composer, StringComparison.Ordinal);
        Assert.Contains("order.cancelled.blocks_action", composer, StringComparison.Ordinal);
        Assert.Contains("order.Status != SellerOrderStatus.Cancelled && fulfillment is not null", composer, StringComparison.Ordinal);
        Assert.Contains("return (policyLabel, \"\", \"before_delivery\")", panel, StringComparison.Ordinal);
    }

    private static CheckoutGroup SeedCheckout(bool cancel)
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
            Guid.NewGuid(),
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
        if (cancel)
        {
            order.Cancel();
        }

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
