using FluentValidation;
using Tooba.BuildingBlocks.Grid;
using Tooba.Fulfillment.Application.Commands.CreateShippingService;
using Tooba.Fulfillment.Application.Commands.DeactivateShippingService;
using Tooba.Fulfillment.Application.Commands.ExecuteAdminFulfillmentBulk;
using Tooba.Fulfillment.Application.Commands.SellerMutateFulfillment;
using Tooba.Fulfillment.Application.Commands.UpdateShippingService;
using Tooba.Fulfillment.Application.Models;
using Tooba.Fulfillment.Application.Queries.GetAdminFulfillment;
using Tooba.Fulfillment.Application.Queries.GetSellerFulfillment;
using Tooba.Fulfillment.Application.Queries.GetShippingService;
using Tooba.Fulfillment.Application.Queries.ListCustomerCheckoutFulfillments;
using Tooba.Fulfillment.Application.Queries.QueryAdminFulfillmentWorkQueue;
using Tooba.Fulfillment.Application.Shipping;
using Tooba.Fulfillment.Application.Validators.Admin;
using Tooba.Fulfillment.Application.Validators.Customer;
using Tooba.Fulfillment.Application.Validators.Seller;
using Tooba.Fulfillment.Application.Validators.Shipping;
using Xunit;

namespace Tooba.Fulfillment.Tests.Architecture;

/// <summary>
/// TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-PRECERT-REPAIR-001 — direct in-memory proof that the
/// added Fulfillment validators enforce primitive transport/input shape only and never police
/// trusted authorizer values or Application/Domain business behavior. No web host, no database.
/// </summary>
public sealed class FulfillmentValidatorBehaviorTests
{
    private static readonly Guid NonEmpty = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Route_identifier_validators_reject_empty_and_accept_non_empty()
    {
        Assert.False(Validate(new GetSellerFulfillmentQuery(NonEmpty, Guid.Empty), new GetSellerFulfillmentQueryValidator()).IsValid);
        Assert.True(Validate(new GetSellerFulfillmentQuery(NonEmpty, NonEmpty), new GetSellerFulfillmentQueryValidator()).IsValid);

        Assert.False(Validate(new GetAdminFulfillmentQuery(Guid.Empty), new GetAdminFulfillmentQueryValidator()).IsValid);
        Assert.True(Validate(new GetAdminFulfillmentQuery(NonEmpty), new GetAdminFulfillmentQueryValidator()).IsValid);

        Assert.False(Validate(new GetShippingServiceQuery(Guid.Empty), new GetShippingServiceQueryValidator()).IsValid);
        Assert.True(Validate(new GetShippingServiceQuery(NonEmpty), new GetShippingServiceQueryValidator()).IsValid);

        Assert.False(Validate(new DeactivateShippingServiceCommand(Guid.Empty), new DeactivateShippingServiceCommandValidator()).IsValid);
        Assert.True(Validate(new DeactivateShippingServiceCommand(NonEmpty), new DeactivateShippingServiceCommandValidator()).IsValid);
    }

    [Fact]
    public void Customer_checkout_query_rejects_empty_checkout_identifier_only()
    {
        var validator = new ListCustomerCheckoutFulfillmentsQueryValidator();
        var result = Validate(new ListCustomerCheckoutFulfillmentsQuery(Guid.Empty), validator);
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);

        Assert.True(Validate(new ListCustomerCheckoutFulfillmentsQuery(NonEmpty), validator).IsValid);
    }

    [Fact]
    public void Seller_mutation_validator_polices_only_command_shape()
    {
        var validator = new SellerMutateFulfillmentCommandValidator();

        Assert.False(Validate(Command(fulfillmentId: Guid.Empty), validator).IsValid);
        Assert.False(Validate(
            new SellerMutateFulfillmentCommand(
                NonEmpty, NonEmpty, NonEmpty, null!, SellerFulfillmentMutationKind.MarkPacked),
            validator).IsValid);
        Assert.False(Validate(Command(shipmentId: Guid.Empty), validator).IsValid);
        Assert.False(Validate(Command(carrierDisplayName: "   "), validator).IsValid);
        Assert.False(Validate(Command(trackingReference: "\t "), validator).IsValid);
        Assert.False(Validate(Command(shippingMethodCode: "  "), validator).IsValid);
        Assert.True(Validate(Command(), validator).IsValid);
    }

    [Fact]
    public void Seller_mutation_validator_rejects_malformed_shipment_lines()
    {
        var validator = new SellerMutateFulfillmentCommandValidator();

        Assert.False(Validate(Command(lines: [null!]), validator).IsValid);
        Assert.False(Validate(Command(lines: [new ShipmentLineCommand(Guid.Empty, 1m)]), validator).IsValid);
        Assert.False(Validate(Command(lines: [new ShipmentLineCommand(NonEmpty, 0m)]), validator).IsValid);
        Assert.True(Validate(Command(lines: [new ShipmentLineCommand(NonEmpty, 1.5m)]), validator).IsValid);
        Assert.True(Validate(Command(lines: []), validator).IsValid);
    }

    [Fact]
    public void Seller_mutation_validator_does_not_police_authorizer_derived_values()
    {
        var validator = new SellerMutateFulfillmentCommandValidator();
        Assert.True(Validate(
            Command(actorUserId: Guid.Empty, sellerPartyId: Guid.Empty),
            validator).IsValid);
    }

    [Fact]
    public void Admin_bulk_validator_only_rejects_null_envelope()
    {
        var validator = new ExecuteAdminFulfillmentBulkCommandValidator();

        Assert.False(Validate(new ExecuteAdminFulfillmentBulkCommand(NonEmpty, null!), validator).IsValid);

        var hostile = new AdminFulfillmentWorkQueueBulkRequest("not_a_supported_action", []);
        Assert.True(Validate(new ExecuteAdminFulfillmentBulkCommand(Guid.Empty, hostile), validator).IsValid);
    }

    [Fact]
    public void Admin_work_queue_validator_only_rejects_null_envelope()
    {
        var validator = new QueryAdminFulfillmentWorkQueueQueryValidator();

        Assert.False(Validate(new QueryAdminFulfillmentWorkQueueQuery(null!), validator).IsValid);

        var policyHostile = new GridQueryRequest(
            0,
            0,
            null,
            [new GridSortRequest("notAField", "sideways")],
            [new GridFilterRequest("notAField", "notAnOperator", "x", null, null)]);
        Assert.True(Validate(new QueryAdminFulfillmentWorkQueueQuery(policyHostile), validator).IsValid);
    }

    [Fact]
    public void Shipping_validators_scope_only_identifiers_and_model_presence()
    {
        var create = new CreateShippingServiceCommandValidator();
        Assert.False(Validate(new CreateShippingServiceCommand(null!), create).IsValid);
        Assert.True(Validate(new CreateShippingServiceCommand(WriteModel("anything")), create).IsValid);

        var update = new UpdateShippingServiceCommandValidator();
        Assert.False(Validate(new UpdateShippingServiceCommand(Guid.Empty, WriteModel("anything")), update).IsValid);
        Assert.False(Validate(new UpdateShippingServiceCommand(NonEmpty, null!), update).IsValid);
        Assert.True(Validate(new UpdateShippingServiceCommand(NonEmpty, WriteModel("anything")), update).IsValid);
    }

    private static ShippingServiceWriteModel WriteModel(string code) =>
        new(code, string.Empty, string.Empty, string.Empty, false, 0, [], []);

    private static SellerMutateFulfillmentCommand Command(
        Guid? fulfillmentId = null,
        Guid? actorUserId = null,
        Guid? sellerPartyId = null,
        SellerHandlePermissionInput? permission = null,
        string? carrierDisplayName = null,
        IReadOnlyList<ShipmentLineCommand>? lines = null,
        Guid? shipmentId = null,
        string? trackingReference = null,
        string? shippingMethodCode = null) =>
        new(
            fulfillmentId ?? NonEmpty,
            actorUserId ?? NonEmpty,
            sellerPartyId ?? NonEmpty,
            permission ?? new SellerHandlePermissionInput(true, []),
            SellerFulfillmentMutationKind.CreateShipment,
            carrierDisplayName,
            lines,
            shipmentId,
            trackingReference,
            shippingMethodCode);

    private static FluentValidation.Results.ValidationResult Validate<T>(
        T instance,
        IValidator<T> validator) => validator.Validate(instance);
}
