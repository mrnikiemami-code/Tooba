using Tooba.Order.Application;
using Tooba.Order.Domain;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// قاعدهٔ authoritative لغو SellerOrder — دامنه + سیاست مشترک.
/// </summary>
public sealed class SellerOrderCancellationGuardTests
{
    [Fact]
    public void Cancel_allows_pending_payment_and_is_idempotent()
    {
        var order = OpenPending();
        order.Cancel();
        Assert.Equal(SellerOrderStatus.Cancelled, order.Status);
        order.Cancel();
        Assert.Equal(SellerOrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Cancel_rejects_paid_without_paid_path()
    {
        var order = OpenPending();
        order.RecordVerifiedPayment();
        var ex = Assert.Throws<InvalidOperationException>(() => order.Cancel());
        Assert.Contains("order.cancel.forbidden", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CancelPaidBeforeShipment_allows_paid_only()
    {
        var order = OpenPending();
        order.RecordVerifiedPayment();
        order.CancelPaidBeforeShipment();
        Assert.Equal(SellerOrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Policy_rejects_paid_when_shipped_or_delivered()
    {
        Assert.False(SellerOrderCancellationPolicy.CanCancel(
            SellerOrderStatus.Paid,
            new SellerOrderCancelFulfillmentSnapshot("Delivered", 1)));
        Assert.False(SellerOrderCancellationPolicy.CanCancel(
            SellerOrderStatus.Paid,
            new SellerOrderCancelFulfillmentSnapshot("ReadyToFulfill", 1)));
        Assert.True(SellerOrderCancellationPolicy.CanCancel(
            SellerOrderStatus.Paid,
            new SellerOrderCancelFulfillmentSnapshot("ReadyToFulfill", 0)));
        Assert.True(SellerOrderCancellationPolicy.CanCancel(SellerOrderStatus.PendingPayment, null));
    }

    [Fact]
    public void Evaluator_source_never_references_settlement()
    {
        var root = FindRepoRoot();
        var src = File.ReadAllText(Path.Combine(
            root,
            "src",
            "backend",
            "Modules",
            "Returns",
            "Tooba.Returns.Infrastructure",
            "ReturnEligibilityEvaluator.cs"));
        Assert.DoesNotContain("Settlement", src, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AdjustFromRefund", src, StringComparison.Ordinal);
    }

    private static SellerOrder OpenPending()
    {
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
        return SellerOrder.Open(
            Guid.NewGuid(),
            Guid.NewGuid(),
            $"SO-{sellerOrderId:N}"[..20],
            OrderMode.OnlinePurchase,
            "IRR",
            [line]);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
