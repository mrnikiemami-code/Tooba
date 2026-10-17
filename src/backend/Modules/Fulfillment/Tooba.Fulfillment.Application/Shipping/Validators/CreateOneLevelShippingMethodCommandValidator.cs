using FluentValidation;
using Tooba.Fulfillment.Application.Shipping.Commands;
using Tooba.Fulfillment.Application.Validators;

namespace Tooba.Fulfillment.Application.Shipping.Validators;

/// <summary>
/// Transport/input shape validation for <see cref="CreateOneLevelShippingMethodCommand"/>.
/// Only primitive shape is checked here; catalog-gate state, canonical code validity, duplicate code
/// or binding, language membership, name bounds and activation stay in Application/Domain so they
/// are enforced even for in-process callers.
/// </summary>
public sealed class CreateOneLevelShippingMethodCommandValidator
    : AbstractValidator<CreateOneLevelShippingMethodCommand>
{
    /// <summary>Registers the primitive-shape rule for the new canonical method code.</summary>
    public CreateOneLevelShippingMethodCommandValidator()
    {
        FulfillmentFluentRules.OptionalNonBlankShape(
            this, x => x.MethodCode, FulfillmentValidationCodes.ShippingMethodCodeRequired);
    }
}
