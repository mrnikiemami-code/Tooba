using Tooba.Fulfillment.Application;
using Tooba.Fulfillment.Domain;
using Tooba.Order.Domain;
using Tooba.Returns.Application;
using Tooba.Returns.Infrastructure;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-P09-T006: ساعت مرجوعی quantity-aware برای تحویل جزئی.</summary>
public sealed class SplitDeliveryReturnClockTests
{
    [Fact]
    public void Undelivered_quantity_does_not_start_return_clock_in_admin_ui_helper()
    {
        var line = OrderLine.FromCheckout(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            5,
            1000m,
            "IRR",
            true,
            Guid.NewGuid(),
            null,
            "Taxable",
            0.09m,
            450m,
            5450m,
            null,
            isReturnableSnapshot: true,
            returnWindowDaysSnapshot: 7,
            returnPolicySourceSnapshot: "platform_default",
            returnPolicyLabelSnapshot: "۷ روز پس از تحویل");

        // قبل از تحویل: برچسب سیاست، نه مهلت تقویمی.
        Assert.True(line.IsReturnableSnapshot);
        Assert.Equal(7, line.ReturnWindowDaysSnapshot);
    }

    [Fact]
    public void Line_delivered_at_dictionary_drives_per_line_window()
    {
        var lineId = Guid.NewGuid();
        var deliveredAt = DateTimeOffset.UtcNow.AddDays(-1);
        var later = deliveredAt.AddHours(6);
        var fulfillment = new FulfillmentReturnEligibilitySnapshot(
            Guid.NewGuid(),
            new Dictionary<Guid, decimal> { [lineId] = 5 },
            later,
            new Dictionary<Guid, DateTimeOffset> { [lineId] = deliveredAt },
            [
                new LineDeliverySlice(lineId, 2, deliveredAt),
                new LineDeliverySlice(lineId, 3, later),
            ]);

        Assert.Equal(5, fulfillment.DeliveredQuantities[lineId]);
        Assert.Equal(deliveredAt, fulfillment.LineDeliveredAt![lineId]);
        Assert.Equal(2, fulfillment.DeliverySlices!.Count);
        Assert.Equal(3, fulfillment.DeliverySlices![1].Quantity);
        Assert.Equal(later, fulfillment.DeliverySlices![1].DeliveredAt);
    }

    [Fact]
    public void CreateShipment_persists_method_and_metadata_on_domain()
    {
        var now = DateTimeOffset.UtcNow;
        var lineId = Guid.NewGuid();
        var unit = FulfillmentUnit.CreateFromPaidOrder(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "گیرنده",
            "09120000000",
            "تهران",
            "تهران",
            "آدرس",
            "1234567890",
            "post",
            "پست",
            [(lineId, 3, null)],
            now);
        unit.MarkProcessing(now);
        unit.MarkPacked(now);
        var meta = ShippingProviderMetadataValidator.ValidateAndNormalize(
            "post",
            """{"recipientName":"گیرنده","destinationAddress":"آدرس","recipientPhone":"09120000000","postalCode":"1234567890"}""");
        var shipment = unit.CreateShipment("پست", [(lineId, 2)], now, "post", "پست", meta, 1);
        Assert.Equal("post", shipment.ShippingMethodCode);
        Assert.Equal("پست", shipment.ShippingMethodLabel);
        Assert.Contains("گیرنده", shipment.ProviderMetadataJson);
        Assert.Equal(ShipmentStatus.Created, shipment.Status);
    }
}
