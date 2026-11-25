using FluentValidation;
using Tooba.Offer.Application.Validation;

namespace Tooba.Offer.Application.Offers.Commands.EnsureOfferInventoryPosition;

/// <summary>
/// Transport shape validation for <see cref="EnsureOfferInventoryPositionCommand"/>.
/// Offer ownership, active-location eligibility and idempotency remain Inventory/Application concerns.
/// </summary>
public sealed class EnsureOfferInventoryPositionCommandValidator : AbstractValidator<EnsureOfferInventoryPositionCommand>
{
    /// <summary>Registers primitive-shape rules for the provisioning command.</summary>
    public EnsureOfferInventoryPositionCommandValidator()
    {
        OfferFluentRules.RequireOfferId(this, x => x.OfferId);

        RuleFor(x => x.LocationId)
            .NotEmpty()
            .WithErrorCode(OfferValidationCodes.LocationIdRequired);
    }
}
