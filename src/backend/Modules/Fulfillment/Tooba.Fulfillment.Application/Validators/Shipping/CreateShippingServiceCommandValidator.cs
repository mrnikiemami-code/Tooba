using FluentValidation;
using Tooba.Fulfillment.Application.Commands.CreateShippingService;

namespace Tooba.Fulfillment.Application.Validators.Shipping;

/// <summary>
/// Transport/input shape validation for <see cref="CreateShippingServiceCommand"/>.
/// Only the write-model envelope null-shape is checked. ShippingServiceSemantic and all
/// shipping directory/domain rules stay in Application/Domain.
/// </summary>
public sealed class CreateShippingServiceCommandValidator : AbstractValidator<CreateShippingServiceCommand>
{
    /// <summary>Registers primitive-shape rules for the create shipping service command.</summary>
    public CreateShippingServiceCommandValidator()
    {
        FulfillmentFluentRules.RequireReference(this, x => x.Model, FulfillmentValidationCodes.ShippingServiceModelRequired);
    }
}
