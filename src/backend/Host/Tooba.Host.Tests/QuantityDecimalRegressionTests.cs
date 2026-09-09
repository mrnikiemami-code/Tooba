using Tooba.BuildingBlocks;
using Tooba.Cart.Domain;
using Tooba.Catalog.Domain;
using Tooba.Fulfillment.Domain;
using Tooba.Inventory.Domain;
using Tooba.Order.Domain;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-P09-T013: مقدار اعشاری سبد/سفارش/موجودی/ارسال.</summary>
public sealed class QuantityDecimalRegressionTests
{
    [Fact]
    public void Inventory_fractional_reserve_keeps_available()
    {
        var now = DateTimeOffset.UtcNow;
        var position = StockPosition.Open(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), now);
        position.SyncQuantities(100m, 1.25m, now);
        Assert.Equal(100m, position.OnHand);
        Assert.Equal(1.25m, position.Reserved);
        Assert.Equal(98.75m, position.Available);
        position.SyncQuantities(100m, 0m, now);
        Assert.Equal(100m, position.Available);
    }

    [Fact]
    public void Cart_and_order_snapshot_one_point_two_five()
    {
        CartLine.EnsureQuantity(1.25m);
        var cartLine = CartLine.Open(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1.25m,
            null,
            1000m,
            "IRR",
            true,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);
        Assert.Equal(1.25m, cartLine.Quantity);
        Assert.Equal("1.25", QuantityDisplay.Format(cartLine.Quantity, 2));

        var orderLine = OrderLine.FromCheckout(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1.25m,
            80000m,
            "IRR",
            true,
            Guid.NewGuid(),
            null,
            "Taxable",
            0.09m,
            9000m,
            109000m,
            null,
            unitOfMeasureIdSnapshot: CanonicalUnits.Kg,
            unitCodeSnapshot: "kg",
            unitDisplaySnapshot: "کیلوگرم",
            quantityDecimalPlacesSnapshot: 2,
            quantityStepSnapshot: null);
        Assert.Equal(1.25m, orderLine.Quantity);
        Assert.Equal(CanonicalUnits.Kg, orderLine.UnitOfMeasureIdSnapshot);
        Assert.Equal("kg", orderLine.UnitCodeSnapshot);
        Assert.Equal("کیلوگرم", orderLine.UnitDisplaySnapshot);
        Assert.Equal(2, orderLine.QuantityDecimalPlacesSnapshot);
        Assert.Null(orderLine.QuantityStepSnapshot);
        Assert.Equal(100000m, orderLine.LineTotalSnapshot);
    }

    [Fact]
    public void Fulfillment_exact_and_partial_decimal_pack()
    {
        var lineId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var unit = FulfillmentUnit.CreateFromPaidOrder(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "گیرنده",
            "+98912",
            "تهران",
            "تهران",
            "آدرس",
            "12345",
            "storefront-default",
            "ارسال",
            [(lineId, 1.25m, Guid.NewGuid())],
            now);
        unit.MarkProcessing(now);
        Assert.Equal(1.25m, unit.Items.Single().QuantityProcessing);
        unit.PackSelections([(lineId, 0.50m)], now);
        Assert.Equal(0.50m, unit.Items.Single().QuantityPacked);
        unit.PackSelections([(lineId, 0.75m)], now);
        Assert.Equal(1.25m, unit.Items.Single().QuantityPacked);
    }
}
