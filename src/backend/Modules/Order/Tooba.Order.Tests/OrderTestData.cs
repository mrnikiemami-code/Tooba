using Tooba.Offer.Contracts.Dtos;
using Tooba.Order.Domain;

namespace Tooba.Order.Tests;

/// <summary>سازندهٔ snapshot سفارش برای تست‌ها؛ بدون DbContext و بدون داده واقعی.</summary>
internal static class OrderTestData
{
    public static CheckoutGroup Checkout(
        decimal unitPrice = 1000m,
        decimal quantity = 2m,
        SellerOrderStatus status = SellerOrderStatus.Submitted,
        params string[] unitCodes)
    {
        var order = SellerOrder(unitPrice, quantity, unitCodes);
        if (status == SellerOrderStatus.Cancelled)
        {
            order.Cancel();
        }

        return Submit(order);
    }

    public static SellerOrder SellerOrder(decimal unitPrice, decimal quantity, params string[] unitCodes)
    {
        var checkoutId = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var seller = Guid.NewGuid();
        var codes = unitCodes.Length == 0 ? ["kg"] : unitCodes;
        var lines = codes
            .Select(code => Line(sellerOrderId, seller, quantity, unitPrice, code))
            .ToArray();
        return Domain.SellerOrder.Open(
            checkoutId,
            seller,
            $"SO-{sellerOrderId:N}"[..20],
            OrderMode.OnlinePurchase,
            "IRR",
            lines);
    }

    public static CheckoutGroup Submit(SellerOrder order) =>
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
            DateTimeOffset.UnixEpoch,
            "گیرنده تست",
            "09120000000",
            "تهران",
            "تهران",
            "آدرس تست",
            "1234567890",
            "post",
            "پست");

    private static OrderLine Line(
        Guid sellerOrderId,
        Guid seller,
        decimal quantity,
        decimal unitPrice,
        string unitCode) =>
        OrderLine.FromCheckout(
            sellerOrderId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            seller,
            quantity,
            unitPrice,
            "IRR",
            true,
            Guid.NewGuid(),
            null,
            "Taxable",
            0.09m,
            0m,
            unitPrice * quantity,
            null,
            unitCodeSnapshot: unitCode,
            quantityDecimalPlacesSnapshot: 2);
}
