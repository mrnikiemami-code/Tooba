using FluentValidation;
using Tooba.Offer.Application.Commands.SetOfferInventory;

namespace Tooba.Offer.Application.Validators;

/// <summary>
/// Transport validation for <see cref="SetOfferInventoryCommand"/>.
/// Offer ownership and stock-write policy remain Inventory business validation. No canonical
/// adjustment-reason length limit exists in the Inventory contract/domain, so only non-blank
/// shape is enforced here and no numeric limit is invented.
/// </summary>
public sealed class SetOfferInventoryCommandValidator : AbstractValidator<SetOfferInventoryCommand>
{
    /// <summary>Registers primitive-shape rules for the inventory write command.</summary>
    public SetOfferInventoryCommandValidator()
    {
        OfferFluentRules.RequireOfferId(this, x => x.OfferId);
        OfferFluentRules.RequireSellerPartyId(this, x => x.SellerPartyId);

        RuleFor(x => x.OnHand)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode(OfferValidationCodes.OnHandMin);

        OfferFluentRules.OptionalNonBlankShape(this, x => x.Reason, OfferValidationCodes.ReasonShape);
    }
}
