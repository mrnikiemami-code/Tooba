using Tooba.Fulfillment.Contracts.Operations;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Policies;
using Tooba.Order.Contracts.Shipping;
using Xunit;

namespace Tooba.Order.Tests.Admin;

/// <summary>
/// TB-252-R1/R2: transport routing for generic create_shipment — CarrierApi never reaches Fulfillment create;
/// Manual binds with trusted server recipient metadata (never client ProviderMetadataJson).
/// </summary>
public sealed class AcceptedGroupCreateShipmentTransportRoutingTests
{
    private static readonly Guid CheckoutId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid SellerOrderId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid SellerPartyId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid FulfillmentId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid GroupA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Line1 = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public void CarrierApi_refuses_before_any_fulfillment_create_call()
    {
        var fulfillmentCalls = 0;
        var facts = new[]
        {
            MakeFacts(GroupA, "tipax:standard", "CarrierApi", hasQuote: true),
        };
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
            "tipax:standard",
            """{"service":"override"}""",
            null,
            null,
            GroupA);

        var refused = SimulateCreateShipmentCore(
            Snapshot(),
            request,
            facts,
            () =>
            {
                fulfillmentCalls++;
                return Task.FromResult<object>(new object());
            });

        Assert.False(refused.Ok);
        Assert.Equal(AcceptedGroupShipmentCreateBinder.CarrierApiUseDedicatedPath, refused.Code);
        Assert.Equal(0, fulfillmentCalls);
    }

    [Fact]
    public void Manual_binds_trusted_metadata_and_refuses_client_provider_json()
    {
        string? capturedMetadata = "sentinel";
        Guid? capturedGroup = null;
        var facts = new[] { MakeFacts(GroupA, "post", "Manual", hasQuote: false) };
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
            """{"should":"be-ignored-by-orchestrator"}""",
            null,
            null,
            GroupA);

        // Binder refuses non-empty client metadata (authority). Orchestrator never forwards it.
        var binderRefuse = Assert.Throws<AdminOrderOperationsException>(() =>
            AcceptedGroupShipmentCreateBinder.Resolve(Snapshot(), request, facts));
        Assert.Equal(AcceptedGroupShipmentCreateBinder.ProviderMetadataForbidden, binderRefuse.Code);

        var cleanRequest = request with { ProviderMetadataJson = null };
        var result = SimulateCreateShipmentCore(
            Snapshot(),
            cleanRequest,
            facts,
            async () =>
            {
                var bound = AcceptedGroupShipmentCreateBinder.Resolve(Snapshot(), cleanRequest, facts);
                capturedGroup = bound.ShippingGroupId;
                capturedMetadata = bound.TrustedProviderMetadataJson;
                await Task.CompletedTask;
                return new object();
            });

        Assert.True(result.Ok);
        Assert.Equal(GroupA, capturedGroup);
        Assert.False(string.IsNullOrWhiteSpace(capturedMetadata));
        Assert.Contains("گیرنده", capturedMetadata!, StringComparison.Ordinal);
        var normalized = Tooba.Fulfillment.Application.Shipping.ShippingProviderMetadataValidator
            .ValidateAndNormalize("post", capturedMetadata);
        Assert.Contains("گیرنده", normalized!, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateShipmentCoreAsync_source_forwards_trusted_not_client_metadata()
    {
        var root = FindRepoRoot();
        var path = Path.Combine(
            root,
            "src",
            "backend",
            "Modules",
            "Order",
            "Tooba.Order.Application",
            "Admin",
            "Operations",
            "Services",
            "AdminOrderOperationsOrchestrator.Fulfillment.cs");
        var source = File.ReadAllText(path);
        Assert.Contains("bound.TrustedProviderMetadataJson", source, StringComparison.Ordinal);
        Assert.DoesNotContain("request.ProviderMetadataJson", source, StringComparison.Ordinal);
        Assert.Contains("AcceptedGroupShipmentCreateBinder.Resolve", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Manual_post_bind_emits_trusted_recipient_metadata_accepted_by_validator()
    {
        var facts = MakeFacts(GroupA, "post", "Manual", hasQuote: false);
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
        Assert.Equal("post", bound.ShippingMethodCode);
        Assert.False(string.IsNullOrWhiteSpace(bound.TrustedProviderMetadataJson));
        Assert.Contains("گیرنده", bound.TrustedProviderMetadataJson!, StringComparison.Ordinal);
        var normalized = Tooba.Fulfillment.Application.Shipping.ShippingProviderMetadataValidator
            .ValidateAndNormalize("post", bound.TrustedProviderMetadataJson);
        Assert.Contains("گیرنده", normalized!, StringComparison.Ordinal);
    }

    private static RoutingOutcome SimulateCreateShipmentCore(
        FulfillmentSnapshot snapshot,
        AdminOrderOperationRequest request,
        IReadOnlyList<OrderCommittedCarrierShipmentFacts> facts,
        Func<Task<object>> onFulfillmentCreate)
    {
        try
        {
            _ = AcceptedGroupShipmentCreateBinder.Resolve(snapshot, request, facts);
        }
        catch (AdminOrderOperationsException ex)
        {
            return new RoutingOutcome(false, ex.Code);
        }

        onFulfillmentCreate().GetAwaiter().GetResult();
        return new RoutingOutcome(true, null);
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

    private static OrderCommittedCarrierShipmentFacts MakeFacts(
        Guid groupId,
        string method,
        string transportMode,
        bool hasQuote) =>
        new(
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
            transportMode == "CarrierApi",
            transportMode == "CarrierApi" ? null : "not_carrier_api",
            [Line1],
            [new OrderCommittedCarrierExecutableLine(Line1, 2m, 1000m, 2m)],
            FulfillmentId,
            OptionCode: transportMode == "Manual" ? null : "standard");

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(
                dir.FullName,
                "src",
                "backend",
                "Modules",
                "Order",
                "Tooba.Order.Application",
                "Admin",
                "Operations",
                "Services",
                "AdminOrderOperationsOrchestrator.Fulfillment.cs");
            if (File.Exists(candidate) && File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }

    private sealed record RoutingOutcome(bool Ok, string? Code);
}
