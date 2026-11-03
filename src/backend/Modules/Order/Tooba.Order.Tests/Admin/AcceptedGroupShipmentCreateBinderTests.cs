using Tooba.Fulfillment.Contracts.Operations;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Policies;
using Tooba.Order.Contracts.Shipping;
using Xunit;

namespace Tooba.Order.Tests.Admin;

public sealed class AcceptedGroupShipmentCreateBinderTests
{
    private static readonly Guid CheckoutId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid SellerOrderId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid SellerPartyId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid FulfillmentId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid GroupA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid GroupB = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Line1 = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid Line2 = Guid.Parse("44444444-4444-4444-4444-444444444444");

    [Fact]
    public void Resolve_derives_method_and_allows_partial_allocation()
    {
        var snapshot = Snapshot();
        var facts = new[] { Facts(GroupA, "post", Line1, 3m, Line2, 2m) };
        var request = Request(GroupA, "post", new AdminOrderLineSelection(Line1, 1m));

        var bound = AcceptedGroupShipmentCreateBinder.Resolve(snapshot, request, facts);

        Assert.Equal(GroupA, bound.ShippingGroupId);
        Assert.Equal("post", bound.ShippingMethodCode);
        Assert.Equal("پیشتاز", bound.CarrierDisplayName);
        Assert.Single(bound.Lines);
        Assert.Equal(Line1, bound.Lines[0].OrderLineId);
        Assert.Equal(1m, bound.Lines[0].Quantity);
    }

    [Fact]
    public void Resolve_refuses_method_override()
    {
        var ex = Assert.Throws<AdminOrderOperationsException>(() =>
            AcceptedGroupShipmentCreateBinder.Resolve(
                Snapshot(),
                Request(GroupA, "tipax", new AdminOrderLineSelection(Line1, 1m)),
                [Facts(GroupA, "post", Line1, 2m)]));
        Assert.Equal(AcceptedGroupShipmentCreateBinder.AcceptedGroupMethodMismatch, ex.Code);
    }

    [Fact]
    public void Resolve_refuses_prefix_option_mismatch()
    {
        var ex = Assert.Throws<AdminOrderOperationsException>(() =>
            AcceptedGroupShipmentCreateBinder.Resolve(
                Snapshot(),
                Request(GroupA, "post:other", new AdminOrderLineSelection(Line1, 1m)),
                [Facts(GroupA, "post", Line1, 2m, optionCode: "standard")]));
        Assert.Equal(AcceptedGroupShipmentCreateBinder.AcceptedGroupMethodMismatch, ex.Code);
    }

    [Fact]
    public void Resolve_refuses_carrier_api_transport()
    {
        var ex = Assert.Throws<AdminOrderOperationsException>(() =>
            AcceptedGroupShipmentCreateBinder.Resolve(
                Snapshot(),
                Request(GroupA, "tipax:standard", new AdminOrderLineSelection(Line1, 1m)),
                [Facts(GroupA, "tipax:standard", Line1, 2m, transportMode: "CarrierApi", hasQuote: true)]));
        Assert.Equal(AcceptedGroupShipmentCreateBinder.CarrierApiUseDedicatedPath, ex.Code);
    }

    [Fact]
    public void Resolve_refuses_provider_metadata_override()
    {
        var ex = Assert.Throws<AdminOrderOperationsException>(() =>
            AcceptedGroupShipmentCreateBinder.Resolve(
                Snapshot(),
                Request(GroupA, "post", new AdminOrderLineSelection(Line1, 1m), providerMetadata: "{}"),
                [Facts(GroupA, "post", Line1, 2m)]));
        Assert.Equal(AcceptedGroupShipmentCreateBinder.ProviderMetadataForbidden, ex.Code);
    }

    [Fact]
    public void Resolve_aggregates_duplicate_line_selections()
    {
        var bound = AcceptedGroupShipmentCreateBinder.Resolve(
            Snapshot(),
            Request(
                GroupA,
                "post",
                null,
                selections:
                [
                    new AdminOrderLineSelection(Line1, 1m),
                    new AdminOrderLineSelection(Line1, 1m),
                ]),
            [Facts(GroupA, "post", Line1, 3m)]);

        Assert.Single(bound.Lines);
        Assert.Equal(2m, bound.Lines[0].Quantity);
    }

    [Fact]
    public void Resolve_refuses_aggregated_excess_quantity()
    {
        var ex = Assert.Throws<AdminOrderOperationsException>(() =>
            AcceptedGroupShipmentCreateBinder.Resolve(
                Snapshot(),
                Request(
                    GroupA,
                    "post",
                    null,
                    selections:
                    [
                        new AdminOrderLineSelection(Line1, 2m),
                        new AdminOrderLineSelection(Line1, 2m),
                    ]),
                [Facts(GroupA, "post", Line1, 3m)]));
        Assert.Equal(AcceptedGroupShipmentCreateBinder.AcceptedGroupQuantityExcess, ex.Code);
    }

    [Fact]
    public void Resolve_refuses_missing_group_id()
    {
        var ex = Assert.Throws<AdminOrderOperationsException>(() =>
            AcceptedGroupShipmentCreateBinder.Resolve(Snapshot(), Request(null, "post", null), [Facts(GroupA, "post", Line1, 1m)]));
        Assert.Equal(AcceptedGroupShipmentCreateBinder.AcceptedGroupRequired, ex.Code);
    }

    [Fact]
    public void Resolve_refuses_legacy_without_groups()
    {
        var ex = Assert.Throws<AdminOrderOperationsException>(() =>
            AcceptedGroupShipmentCreateBinder.Resolve(Snapshot(), Request(GroupA, "post", null), []));
        Assert.Equal(AcceptedGroupShipmentCreateBinder.AcceptedGroupUnavailable, ex.Code);
    }

    [Fact]
    public void Resolve_refuses_foreign_group()
    {
        var ex = Assert.Throws<AdminOrderOperationsException>(() =>
            AcceptedGroupShipmentCreateBinder.Resolve(
                Snapshot(),
                Request(GroupB, "post", null),
                [Facts(GroupA, "post", Line1, 1m)]));
        Assert.Equal(AcceptedGroupShipmentCreateBinder.AcceptedGroupForeign, ex.Code);
    }

    [Fact]
    public void Resolve_refuses_cross_group_line()
    {
        var facts = new[]
        {
            Facts(GroupA, "post", Line1, 2m),
            Facts(GroupB, "post", Line2, 2m),
        };
        var ex = Assert.Throws<AdminOrderOperationsException>(() =>
            AcceptedGroupShipmentCreateBinder.Resolve(
                Snapshot(),
                Request(GroupA, "post", new AdminOrderLineSelection(Line2, 1m)),
                facts));
        Assert.Equal(AcceptedGroupShipmentCreateBinder.AcceptedGroupLineForeign, ex.Code);
    }

    [Fact]
    public void Resolve_refuses_excess_quantity()
    {
        var ex = Assert.Throws<AdminOrderOperationsException>(() =>
            AcceptedGroupShipmentCreateBinder.Resolve(
                Snapshot(),
                Request(GroupA, "post", new AdminOrderLineSelection(Line1, 9m)),
                [Facts(GroupA, "post", Line1, 2m)]));
        Assert.Equal(AcceptedGroupShipmentCreateBinder.AcceptedGroupQuantityExcess, ex.Code);
    }

    [Fact]
    public void Exact_identity_accepts_matching_option_composite()
    {
        var bound = AcceptedGroupShipmentCreateBinder.Resolve(
            Snapshot(),
            Request(GroupA, "post:standard", new AdminOrderLineSelection(Line1, 1m)),
            [Facts(GroupA, "post", Line1, 2m, optionCode: "standard")]);
        Assert.Equal(GroupA, bound.ShippingGroupId);
        Assert.Equal("post", bound.ShippingMethodCode);
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
            [
                new FulfillmentItemSnapshot(Guid.NewGuid(), Line1, 3m, 0m, null, 3m, 3m),
                new FulfillmentItemSnapshot(Guid.NewGuid(), Line2, 2m, 0m, null, 2m, 2m),
            ],
            []);

    private static AdminOrderOperationRequest Request(
        Guid? groupId,
        string? method,
        AdminOrderLineSelection? selection,
        string? providerMetadata = null,
        IReadOnlyList<AdminOrderLineSelection>? selections = null) =>
        new(
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
            selections ?? (selection is null ? null : [selection]),
            method,
            providerMetadata,
            null,
            null,
            groupId);

    private static OrderCommittedCarrierShipmentFacts Facts(
        Guid groupId,
        string method,
        Guid lineA,
        decimal qtyA,
        Guid? lineB = null,
        decimal qtyB = 0m,
        string transportMode = "Manual",
        bool hasQuote = false,
        string? optionCode = null,
        string? serviceCode = null)
    {
        var lines = new List<OrderCommittedCarrierExecutableLine>
        {
            new(lineA, qtyA, 1000m, qtyA),
        };
        var ids = new List<Guid> { lineA };
        if (lineB is Guid b)
        {
            lines.Add(new OrderCommittedCarrierExecutableLine(b, qtyB, 1000m, qtyB));
            ids.Add(b);
        }

        return new OrderCommittedCarrierShipmentFacts(
            CheckoutId,
            groupId,
            SellerPartyId,
            SellerOrderId,
            method,
            "پیشتاز",
            transportMode,
            0,
            "store-alpha",
            null,
            "گیرنده",
            "09120000000",
            "آدرس",
            "1234567890",
            "تهران",
            "تهران",
            "IRR",
            125000m,
            0m,
            hasQuote,
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
            transportMode == "CarrierApi" && hasQuote,
            transportMode == "CarrierApi" ? null : "not_carrier_api",
            ids,
            lines,
            FulfillmentId,
            ServiceCode: serviceCode,
            OptionCode: optionCode);
    }
}
