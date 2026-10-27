using FluentValidation;
using Tooba.Offer.Application.Validation;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Dtos;

namespace Tooba.Offer.Application.Offers.Commands.AssignOfferFulfillment;

/// <summary>
/// Transport validation for <see cref="AssignOfferFulfillmentCommand"/>.
/// </summary>
/// <remarks>
/// Only primitive shape is checked here. Offer existence, the archived rule, the "type without responsibility"
/// rule and the real active Fulfillment taxonomy stay business validation inside the handler.
/// </remarks>
public sealed class AssignOfferFulfillmentCommandValidator : AbstractValidator<AssignOfferFulfillmentCommand>
{
    /// <summary>Registers the shape rules.</summary>
    public AssignOfferFulfillmentCommandValidator()
    {
        RuleFor(x => x.OfferId)
            .NotEmpty()
            .WithErrorCode(OfferValidationCodes.OfferIdRequired);

        RuleFor(x => x.Responsibility)
            .Must(value => value is null || value is OfferFulfillmentResponsibility.Seller or OfferFulfillmentResponsibility.Platform)
            .WithErrorCode(OfferValidationCodes.FulfillmentResponsibilityShape);

        RuleFor(x => x.ShippingTypeId)
            .Must(value => value is null || value.Value != Guid.Empty)
            .WithErrorCode(OfferValidationCodes.FulfillmentShippingTypeIdShape);
    }
}

