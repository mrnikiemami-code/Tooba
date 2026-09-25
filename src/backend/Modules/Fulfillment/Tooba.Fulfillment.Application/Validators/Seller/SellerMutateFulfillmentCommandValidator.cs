using FluentValidation;
using Tooba.Fulfillment.Application.Commands.SellerMutateFulfillment;

namespace Tooba.Fulfillment.Application.Validators.Seller;

/// <summary>
/// Transport/input shape validation for <see cref="SellerMutateFulfillmentCommand"/>.
/// ActorUserId, SellerPartyId, seller ownership, fulfillment existence/state, mutation-kind
/// requirements, shipping-method existence and tracking/provider semantics stay in the
/// seller authorization boundary and Application/Domain. Only Permission non-null is a
/// command-shape invariant.
/// </summary>
public sealed class SellerMutateFulfillmentCommandValidator : AbstractValidator<SellerMutateFulfillmentCommand>
{
    /// <summary>Registers primitive-shape rules for the seller mutation command.</summary>
    public SellerMutateFulfillmentCommandValidator()
    {
        FulfillmentFluentRules.RequireId(this, x => x.FulfillmentId, FulfillmentValidationCodes.FulfillmentIdRequired);
        FulfillmentFluentRules.RequireReference(this, x => x.Permission, FulfillmentValidationCodes.SellerPermissionRequired);
        FulfillmentFluentRules.OptionalIdShape(this, x => x.ShipmentId, FulfillmentValidationCodes.ShipmentIdShape);
        FulfillmentFluentRules.OptionalNonBlankShape(this, x => x.CarrierDisplayName, FulfillmentValidationCodes.CarrierDisplayNameShape);
        FulfillmentFluentRules.OptionalNonBlankShape(this, x => x.TrackingReference, FulfillmentValidationCodes.TrackingReferenceShape);
        FulfillmentFluentRules.OptionalNonBlankShape(this, x => x.ShippingMethodCode, FulfillmentValidationCodes.ShippingMethodCodeShape);

        When(x => x.ShipmentLines is not null, () =>
        {
            RuleFor(x => x.ShipmentLines!)
                .Must(lines => lines.All(line => line is not null))
                .WithErrorCode(FulfillmentValidationCodes.ShipmentLinesNoNullItems);

            RuleFor(x => x.ShipmentLines!)
                .Must(lines => lines.All(line => line is null || line.OrderLineId != Guid.Empty))
                .WithErrorCode(FulfillmentValidationCodes.ShipmentLineOrderLineIdRequired);

            RuleFor(x => x.ShipmentLines!)
                .Must(lines => lines.All(line => line is null || line.Quantity > 0m))
                .WithErrorCode(FulfillmentValidationCodes.ShipmentLineQuantityPositive);
        });
    }
}
