using FluentValidation;
using Tooba.Fulfillment.Application.Shipping.Commands;
using Tooba.Fulfillment.Application.Validators;

namespace Tooba.Fulfillment.Application.Shipping.Validators;

/// <summary>
/// Transport/input shape validation for <see cref="UpdateOneLevelShippingMethodTranslationCommand"/>.
/// Only the primitive shape of the supplied method code is checked here; whether the code exists,
/// whether the gate is enabled, whether the language is known and whether the label is a bounded
/// non-blank human name stay in Application/Domain.
/// </summary>
public sealed class UpdateOneLevelShippingMethodTranslationCommandValidator
    : AbstractValidator<UpdateOneLevelShippingMethodTranslationCommand>
{
    /// <summary>Registers the primitive-shape rule for the one-level method code.</summary>
    public UpdateOneLevelShippingMethodTranslationCommandValidator()
    {
        FulfillmentFluentRules.OptionalNonBlankShape(
            this, x => x.MethodCode, FulfillmentValidationCodes.ShippingMethodCodeRequired);
    }
}
