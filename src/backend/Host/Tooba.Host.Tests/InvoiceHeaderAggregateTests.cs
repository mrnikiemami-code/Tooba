using Tooba.BuildingBlocks;
using Tooba.Order.Domain;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-P09-T014: جمع هدر از خطوط نهایی؛ بدون گرد کردن دوبارهٔ جمع.</summary>
public sealed class InvoiceHeaderAggregateTests
{
    [Fact]
    public void Header_sums_finalized_line_values_without_extra_round()
    {
        var sellerOrderId = Guid.NewGuid();
        var checkoutId = Guid.NewGuid();
        var seller = Guid.NewGuid();
        var discount = FinancialRounder.Round(998m * 0.20m, 0, QuantityRoundingMode.Floor);
        var line = OrderLine.FromCheckout(
            sellerOrderId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            seller,
            1m,
            998m,
            "IRR",
            true,
            Guid.NewGuid(),
            null,
            "Taxable",
            0.20m,
            0m,
            799m,
            null,
            discountAmount: discount,
            postDiscountTaxExclusive: 799m,
            dutyAmountSnapshot: 0m);
        var order = SellerOrder.Open(
            checkoutId,
            seller,
            "INV-T014",
            OrderMode.OnlinePurchase,
            "IRR",
            [line],
            QuantityRoundingMode.Floor,
            0);
        Assert.Equal(199m, line.DiscountAmountSnapshot);
        Assert.Equal(998m, order.SubtotalSnapshot);
        Assert.Equal(199m, order.DiscountSnapshot);
        Assert.Equal(799m, order.NetAmountBeforeTax);
        Assert.Equal(0m, order.TaxSnapshot);
        Assert.Equal(0m, order.TotalDutyAmount);
        Assert.Equal(0m, order.TotalTaxAndDutyAmount);
        Assert.Equal(799m, order.GrandTotalSnapshot);
        Assert.Equal(1, order.TotalItemCount);
        Assert.Equal(1m, order.TotalQuantity);
        Assert.Equal("Floor", order.RoundingModeUsed);
        Assert.Equal(0, order.MoneyDecimalPlacesUsed);
    }

    [Fact]
    public void Header_tax_and_duty_stay_separate_and_sum_exactly()
    {
        var sellerOrderId = Guid.NewGuid();
        var seller = Guid.NewGuid();
        var a = OrderLine.FromCheckout(
            sellerOrderId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            seller,
            1m,
            100m,
            "IRR",
            true,
            Guid.NewGuid(),
            null,
            "Taxable",
            0.09m,
            9m,
            109m,
            null,
            dutyAmountSnapshot: 2m);
        var b = OrderLine.FromCheckout(
            sellerOrderId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            seller,
            2m,
            50m,
            "IRR",
            true,
            Guid.NewGuid(),
            null,
            "Taxable",
            0.09m,
            9m,
            109m,
            null,
            dutyAmountSnapshot: 1m);
        var order = SellerOrder.Open(
            Guid.NewGuid(),
            seller,
            "INV-T014-2",
            OrderMode.OnlinePurchase,
            "IRR",
            [a, b],
            QuantityRoundingMode.Nearest,
            0);
        Assert.Equal(18m, order.TaxSnapshot);
        Assert.Equal(3m, order.TotalDutyAmount);
        Assert.Equal(21m, order.TotalTaxAndDutyAmount);
        Assert.Equal(order.TaxSnapshot + order.TotalDutyAmount, order.TotalTaxAndDutyAmount);
        Assert.Equal(2, order.TotalItemCount);
        Assert.Equal(3m, order.TotalQuantity);
        Assert.Equal(a.LineTotalSnapshot + b.LineTotalSnapshot, order.SubtotalSnapshot);
        Assert.Equal(order.NetAmountBeforeTax + order.TotalTaxAndDutyAmount, order.GrandTotalSnapshot);
    }

    [Fact]
    public void Snapshot_fields_do_not_change_when_mode_enum_is_later_different()
    {
        var seller = Guid.NewGuid();
        var line = OrderLine.FromCheckout(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            seller,
            1m,
            10m,
            "IRR",
            true,
            Guid.NewGuid(),
            null,
            "Taxable",
            0m,
            0m,
            10m,
            null);
        var order = SellerOrder.Open(
            Guid.NewGuid(),
            seller,
            "INV-SNAP",
            OrderMode.OnlinePurchase,
            "IRR",
            [line],
            QuantityRoundingMode.Floor,
            0);
        Assert.Equal("Floor", order.RoundingModeUsed);
        Assert.Equal(0, order.MoneyDecimalPlacesUsed);
        Assert.Equal(10m, order.GrandTotalSnapshot);
        Assert.NotEqual("Ceiling", order.RoundingModeUsed);
    }
}
