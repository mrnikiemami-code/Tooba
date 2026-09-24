using FluentValidation;
using Tooba.Offer.Application.Commands.CreateOffer;
using Tooba.Offer.Contracts.Dtos;

namespace Tooba.Offer.Application.Validators;

/// <summary>
/// Transport validation for <see cref="CreateOfferCommand"/>.
/// Catalog/seller existence, DB uniqueness, return-policy governance and domain quantity
/// invariants remain business validation and are not duplicated here.
/// </summary>
public sealed class CreateOfferCommandValidator : AbstractValidator<CreateOfferCommand>
{
    private static readonly string[] AllowedCreateStatuses =
    [
        nameof(OfferStatus.Draft),
        nameof(OfferStatus.Active),
    ];

    /// <summary>Registers primitive-shape rules for the create command.</summary>
    public CreateOfferCommandValidator()
    {
        OfferFluentRules.RequireCatalogVariantId(this, x => x.CatalogVariantId);
        OfferFluentRules.RequireSellerPartyId(this, x => x.SellerPartyId);
        OfferFluentRules.RequireDefinedEnum(this, x => x.Channel);
        OfferFluentRules.OptionalSellerSkuShape(this, x => x.SellerSku);
        OfferFluentRules.OptionalStatusIn(this, x => x.Status, AllowedCreateStatuses, OfferValidationCodes.CreateStatusShape);
        OfferFluentRules.OptionalReturnPolicyChoiceShape(this, x => x.ReturnPolicyChoice);
        OfferFluentRules.OptionalCustomReturnWindowMin(this, x => x.CustomReturnWindowDays);
        OfferFluentRules.OptionalOrderQuantityLimits(
            this,
            x => x.MinimumOrderQuantity,
            x => x.MaximumOrderQuantity);
    }
}
