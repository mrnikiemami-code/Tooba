using System.Text.Json;
using Tooba.Fulfillment.Application.Shipping;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Policies;
using Tooba.Order.Contracts.Shipping;
using Tooba.Fulfillment.Contracts.Operations;
using Xunit;

namespace Tooba.Order.Tests.Admin;

/// <summary>
/// TB-252-R3: trusted metadata must not invent pickup/origin from recipient destination.
/// </summary>
public sealed class AcceptedGroupTrustedOriginMetadataTests
{
    private static readonly Guid CheckoutId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
    private static readonly Guid SellerOrderId = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb");
    private static readonly Guid SellerPartyId = Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc");
    private static readonly Guid GroupA = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid Line1 = Guid.Parse("33333333-3333-4333-8333-333333333333");
    private static readonly Guid FulfillmentId = Guid.Parse("dddddddd-dddd-4ddd-8ddd-dddddddddddd");

    [Fact]
    public void Courier_uses_distinct_origin_and_destination_when_both_authoritative()
    {
        var facts = MakeFacts("snapp_courier") with
        {
            RecipientAddress = "مقصد گیرنده خیابان الف",
            OriginPostalAddress = "مبدأ فروشنده انبار ب",
        };

        var json = AcceptedGroupTrustedProviderMetadata.BuildFromFacts("snapp_courier", facts);
        Assert.NotNull(json);
        using var doc = JsonDocument.Parse(json!);
        Assert.Equal("مبدأ فروشنده انبار ب", doc.RootElement.GetProperty("pickupAddress").GetString());
        Assert.Equal("مقصد گیرنده خیابان الف", doc.RootElement.GetProperty("destinationAddress").GetString());
        Assert.NotEqual(
            doc.RootElement.GetProperty("pickupAddress").GetString(),
            doc.RootElement.GetProperty("destinationAddress").GetString());

        var normalized = ShippingProviderMetadataValidator.ValidateAndNormalize("snapp_courier", json);
        Assert.Contains("مبدأ فروشنده انبار ب", normalized!, StringComparison.Ordinal);
        Assert.Contains("مقصد گیرنده خیابان الف", normalized!, StringComparison.Ordinal);
    }

    [Fact]
    public void Courier_refuses_when_origin_absent_even_if_recipient_present()
    {
        var facts = MakeFacts("snapp_courier") with
        {
            RecipientAddress = "فقط مقصد گیرنده",
            OriginPostalAddress = null,
        };

        var ex = Assert.Throws<AcceptedGroupParcelCreateException>(() =>
            AcceptedGroupTrustedProviderMetadata.BuildFromFacts("snapp_courier", facts));
        Assert.Equal("fulfillment.shipping.courier.address_required", ex.Code);
    }

    [Fact]
    public void In_person_refuses_when_collection_absent_even_if_recipient_address_city_present()
    {
        var facts = MakeFacts("in_person") with
        {
            RecipientAddress = "آدرس گیرنده",
            CityName = "تهران",
            CollectionLocation = null,
        };

        var ex = Assert.Throws<AcceptedGroupParcelCreateException>(() =>
            AcceptedGroupTrustedProviderMetadata.BuildFromFacts("in_person", facts));
        Assert.Equal("fulfillment.shipping.pickup.location_required", ex.Code);
    }

    [Fact]
    public void In_person_uses_authoritative_collection_location_only()
    {
        var facts = MakeFacts("in_person") with
        {
            RecipientAddress = "نباید استفاده شود",
            CityName = "نباید",
            CollectionLocation = "باجه فروشگاه آرمان",
        };

        var json = AcceptedGroupTrustedProviderMetadata.BuildFromFacts("in_person", facts);
        Assert.NotNull(json);
        Assert.Contains("باجه فروشگاه آرمان", json!, StringComparison.Ordinal);
        Assert.DoesNotContain("نباید", json!, StringComparison.Ordinal);
        ShippingProviderMetadataValidator.ValidateAndNormalize("in_person", json);
    }

    [Fact]
    public void Unknown_registry_code_refuses_unsupported()
    {
        var facts = MakeFacts("post");
        var ex = Assert.Throws<AcceptedGroupParcelCreateException>(() =>
            AcceptedGroupTrustedProviderMetadata.BuildFromFacts("not_a_real_method", facts));
        Assert.Equal("fulfillment.shipping_method.unsupported", ex.Code);
    }

    [Fact]
    public void Store_courier_emits_null_matching_validator_optional_fields()
    {
        var facts = MakeFacts("store_courier");
        var json = AcceptedGroupTrustedProviderMetadata.BuildFromFacts("store_courier", facts);
        Assert.Null(json);
        var normalized = ShippingProviderMetadataValidator.ValidateAndNormalize("store_courier", null);
        Assert.False(string.IsNullOrWhiteSpace(normalized));
    }

    [Fact]
    public void Binder_post_trusted_metadata_is_accepted_by_fulfillment_validator()
    {
        var facts = MakeFacts("post");
        var request = new AdminOrderOperationRequest(
            "create_shipment",
            SellerOrderId,
            FulfillmentId,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            [new AdminOrderLineSelection(Line1, 1m)],
            "post",
            null,
            null,
            null,
            GroupA);

        var bound = AcceptedGroupShipmentCreateBinder.Resolve(Snapshot(), request, [facts]);
        Assert.False(string.IsNullOrWhiteSpace(bound.TrustedProviderMetadataJson));
        var normalized = ShippingProviderMetadataValidator.ValidateAndNormalize(
            "post",
            bound.TrustedProviderMetadataJson);
        Assert.Contains("گیرنده", normalized!, StringComparison.Ordinal);
    }

    private static FulfillmentSnapshot Snapshot() =>
        new(
            FulfillmentId,
            SellerOrderId,
            CheckoutId,
            SellerPartyId,
            FulfillmentOperationStatus.Packed,
            "گیرنده",
            "09120000000",
            "تهران",
            "تهران",
            "آدرس",
            "1234567890",
            "post",
            "پیشتاز",
            [new FulfillmentItemSnapshot(Guid.NewGuid(), Line1, 2m, 0m, null, 2m, 2m)],
            []);

    private static OrderCommittedCarrierShipmentFacts MakeFacts(string method) =>
        new(
            CheckoutId,
            GroupA,
            SellerPartyId,
            SellerOrderId,
            method,
            "برچسب",
            "Manual",
            0,
            "store-alpha",
            null,
            "گیرنده",
            "09120000000",
            "آدرس گیرنده",
            "1234567890",
            "تهران",
            "تهران",
            "IRR",
            125000m,
            0m,
            false,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            "Paid",
            false,
            false,
            false,
            "not_carrier_api",
            [Line1],
            [new OrderCommittedCarrierExecutableLine(Line1, 2m, 1000m, 2m)],
            FulfillmentId);
}
