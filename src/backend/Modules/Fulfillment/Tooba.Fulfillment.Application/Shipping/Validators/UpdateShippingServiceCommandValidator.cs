using FluentValidation;
using Tooba.Fulfillment.Application.Shipping.Commands;
using Tooba.Fulfillment.Application.Validators;

namespace Tooba.Fulfillment.Application.Shipping.Validators;

/// <summary>
/// Transport/input shape validation for <see cref="UpdateShippingServiceCommand"/>.
/// Only the service id and write-model envelope shapes are checked. ShippingServiceSemantic and
/// all shipping directory/domain rules stay in Application/Domain.
/// </summary>
public sealed class UpdateShippingServiceCommandValidator : AbstractValidator<UpdateShippingServiceCommand>
{
    /// <summary>Registers primitive-shape rules for the update shipping service command.</summary>
    public UpdateShippingServiceCommandValidator()
    {
        FulfillmentFluentRules.RequireId(this, x => x.ServiceId, FulfillmentValidationCodes.ShippingServiceIdRequired);
        FulfillmentFluentRules.RequireReference(this, x => x.Model, FulfillmentValidationCodes.ShippingServiceModelRequired);
    }
}
