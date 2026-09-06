using Tooba.Fulfillment.Domain;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>تست دامنه: جلوگیری از ایجاد محمولهٔ تکراری قبل از dispatch.</summary>
public sealed class FulfillmentShipmentAllocationTests
{
    [Fact]
    public void CreateShipment_rejects_over_allocation_while_prior_shipment_still_Created()
    {
        var sellerOrderId = Guid.NewGuid();
        var checkoutId = Guid.NewGuid();
        var sellerPartyId = Guid.NewGuid();
        var orderLineId = Guid.NewGuid();
        var now = DateTimeOffset.Parse("2026-09-06T22:00:00Z");
        var unit = FulfillmentUnit.CreateFromPaidOrder(
            sellerOrderId,
            checkoutId,
            sellerPartyId,
            Guid.NewGuid(),
            "گیرنده",
            "+98912",
            "تهران",
            "تهران",
            "آدرس",
            "12345",
            "storefront-default",
            "ارسال",
            [(orderLineId, 1, Guid.NewGuid())],
            now);

        unit.MarkProcessing(now);
        unit.MarkPacked(now);
        _ = unit.CreateShipment("Carrier A", [(orderLineId, 1)], now);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            unit.CreateShipment("Carrier B", [(orderLineId, 1)], now));
        Assert.Contains("باقیمانده", ex.Message, StringComparison.Ordinal);
        Assert.Single(unit.Shipments);
    }
}
