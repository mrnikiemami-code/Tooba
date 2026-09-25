using FluentValidation;
using Tooba.Fulfillment.Application.Commands.DeactivateShippingService;

namespace Tooba.Fulfillment.Application.Validators.Shipping;

/// <summary>
/// Transport/input shape validation for <see cref="DeactivateShippingServiceCommand"/>.
/// Existence and deactivation business rules stay in Application/Domain.
/// </summary>
public sealed class DeactivateShippingServiceCommandValidator
    : AbstractValidator<DeactivateShippingServiceCommand>
{
    /// <summary>Registers primitive-shape rules for the deactivate shipping service command.</summary>
    public DeactivateShippingServiceCommandValidator()
    {
        FulfillmentFluentRules.RequireId(this, x => x.ServiceId, FulfillmentValidationCodes.ShippingServiceIdRequired);
    }
}
