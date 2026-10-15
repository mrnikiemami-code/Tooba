using FluentValidation;
using Tooba.Fulfillment.Application.Shipping.Queries;
using Tooba.Fulfillment.Application.Validators;

namespace Tooba.Fulfillment.Application.Shipping.Validators;

/// <summary>
/// Transport/input shape validation for <see cref="ResolveOneLevelShippingMethodBindingQuery"/>.
/// Only the primitive shape of the supplied method code is checked here; whether the code exists,
/// whether the gate is enabled and whether the legacy binding resolves stay in Application/Domain.
/// </summary>
public sealed class ResolveOneLevelShippingMethodBindingQueryValidator
    : AbstractValidator<ResolveOneLevelShippingMethodBindingQuery>
{
    /// <summary>Registers the primitive-shape rule for the one-level method code.</summary>
    public ResolveOneLevelShippingMethodBindingQueryValidator()
    {
        FulfillmentFluentRules.OptionalNonBlankShape(
            this, x => x.MethodCode, FulfillmentValidationCodes.ShippingMethodCodeRequired);
    }
}
