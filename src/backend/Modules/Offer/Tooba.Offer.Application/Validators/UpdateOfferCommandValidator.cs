using FluentValidation;
using Tooba.Offer.Application.Commands.UpdateOffer;
using Tooba.Offer.Contracts.Dtos;

namespace Tooba.Offer.Application.Validators;

/// <summary>
/// Transport validation for <see cref="UpdateOfferCommand"/>.
/// Patch members stay optional; only their supplied shape is checked.
/// </summary>
public sealed class UpdateOfferCommandValidator : AbstractValidator<UpdateOfferCommand>
{
    private static readonly string[] AllowedPatchStatuses =
    [
        nameof(OfferStatus.Active),
        nameof(OfferStatus.Suspended),
        nameof(OfferStatus.Archived),
    ];

    /// <summary>Registers primitive-shape rules for the patch command.</summary>
    public UpdateOfferCommandValidator()
    {
        OfferFluentRules.RequireOfferId(this, x => x.OfferId);
        OfferFluentRules.RequireSellerPartyId(this, x => x.SellerPartyId);
        OfferFluentRules.OptionalSellerSkuShape(this, x => x.SellerSku);
        OfferFluentRules.OptionalStatusIn(this, x => x.Status, AllowedPatchStatuses, OfferValidationCodes.UpdateStatusShape);
        OfferFluentRules.OptionalReturnPolicyChoiceShape(this, x => x.ReturnPolicyChoice);
        OfferFluentRules.OptionalCustomReturnWindowMin(this, x => x.CustomReturnWindowDays);
        OfferFluentRules.OptionalOrderQuantityLimits(
            this,
            x => x.MinimumOrderQuantity,
            x => x.MaximumOrderQuantity);
    }
}
