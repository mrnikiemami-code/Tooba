using Tooba.Host.Admin;
using Tooba.Host.Grid;
using Tooba.Offer.Domain;
using Tooba.Order.Domain;
using Tooba.Returns.Domain;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-P09-T014-R1: تعداد ردیف صحیح از جمع مقدار جدا است.</summary>
public sealed class InvoiceHeaderSemanticsTests
{
    [Fact]
    public void Three_fractional_lines_keep_integer_count_and_decimal_quantity()
    {
        var order = OpenThreeLines(1.25m, 2.00m, 1m, "kg", "kg", "kg");
        Assert.Equal(3, order.TotalItemCount);
        Assert.Equal(4.25m, order.TotalQuantity);
        Assert.Equal(3, InvoiceHeaderSemantics.LineCount(order));
        Assert.NotEqual(order.TotalQuantity, InvoiceHeaderSemantics.LineCount(order));
        Assert.Equal(order.TaxSnapshot + order.TotalDutyAmount, order.TotalTaxAndDutyAmount);
        Assert.Equal(0m, order.TotalDutyAmount);
    }

    [Fact]
    public void List_projection_reads_header_count_not_quantity()
    {
        var group = Submit(OpenThreeLines(1.25m, 2.00m, 1m, "kg", "kg", "kg"));
        var item = AdminOrdersGridQueryEngine.MapOrderListItem(
            group,
            new Dictionary<Guid, string>(),
            new Dictionary<Guid, IReadOnlyList<ReturnRequest>>());
        Assert.Equal(3, item.LineCount);
        Assert.NotEqual(4.25m, item.LineCount);
    }

    [Fact]
    public void Invoice_html_uses_integer_count_and_omits_mixed_unit_quantity()
    {
        var same = AdminOrderCompletenessComposer.RenderInvoiceHtml(
            Submit(OpenThreeLines(1.25m, 2.00m, 1m, "kg", "kg", "kg")),
            payment: null);
        Assert.Contains("تعداد اقلام: <strong dir=\"ltr\">3</strong>", same, StringComparison.Ordinal);
        Assert.Contains("جمع مقدار: <strong dir=\"ltr\">4.25</strong>", same, StringComparison.Ordinal);
        Assert.DoesNotContain("4.250000", same, StringComparison.Ordinal);

        var mixed = AdminOrderCompletenessComposer.RenderInvoiceHtml(
            Submit(OpenThreeLines(1.25m, 2.00m, 1m, "kg", "pcs", "kg")),
            payment: null);
        Assert.Contains("تعداد اقلام: <strong dir=\"ltr\">3</strong>", mixed, StringComparison.Ordinal);
        Assert.DoesNotContain("جمع مقدار", mixed, StringComparison.Ordinal);
    }

    private static SellerOrder OpenThreeLines(
        decimal a,
        decimal b,
        decimal c,
        string unitA,
        string unitB,
        string unitC)
    {
        var sellerOrderId = Guid.NewGuid();
        var seller = Guid.NewGuid();
        return SellerOrder.Open(
            Guid.NewGuid(),
            seller,
            "INV-R1",
            OrderMode.OnlinePurchase,
            "IRR",
            [
                Line(sellerOrderId, seller, a, unitA),
                Line(sellerOrderId, seller, b, unitB),
                Line(sellerOrderId, seller, c, unitC)
            ]);
    }

    private static OrderLine Line(Guid sellerOrderId, Guid seller, decimal qty, string unit) =>
        OrderLine.FromCheckout(
            sellerOrderId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            seller,
            qty,
            10m,
            "IRR",
            true,
            Guid.NewGuid(),
            null,
            "Taxable",
            0.09m,
            0m,
            10m * qty,
            null,
            unitCodeSnapshot: unit,
            quantityDecimalPlacesSnapshot: 2);

    private static CheckoutGroup Submit(SellerOrder order) =>
        CheckoutGroup.Submit(
            order.CheckoutId,
            $"idem-{order.CheckoutId:N}",
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
