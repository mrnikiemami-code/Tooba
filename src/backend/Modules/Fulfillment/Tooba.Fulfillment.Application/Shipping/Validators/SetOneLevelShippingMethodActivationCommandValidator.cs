using FluentValidation;
using Tooba.Fulfillment.Application.Shipping.Commands;
using Tooba.Fulfillment.Application.Validators;

namespace Tooba.Fulfillment.Application.Shipping.Validators;

/// <summary>
/// Transport/input shape validation for <see cref="SetOneLevelShippingMethodActivationCommand"/>.
/// Only the primitive shape of the supplied method code is checked here; whether the code exists,
/// whether the gate is enabled, whether the legacy parent/option exist and whether the method is
/// option-backed stay in Application/Domain.
/// </summary>
public sealed class SetOneLevelShippingMethodActivationCommandValidator
    : AbstractValidator<SetOneLevelShippingMethodActivationCommand>
{
    /// <summary>Registers the primitive-shape rule for the one-level method code.</summary>
    public SetOneLevelShippingMethodActivationCommandValidator()
    {
        FulfillmentFluentRules.OptionalNonBlankShape(
            this, x => x.MethodCode, FulfillmentValidationCodes.ShippingMethodCodeRequired);
    }
}
