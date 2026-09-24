using FluentValidation;
using Tooba.Offer.Application.Commands.SetOfferPrice;

namespace Tooba.Offer.Application.Validators;

/// <summary>
/// Transport validation for <see cref="SetOfferPriceCommand"/>.
/// Active-price existence and market policy remain Pricing business validation.
/// </summary>
public sealed class SetOfferPriceCommandValidator : AbstractValidator<SetOfferPriceCommand>
{
    /// <summary>Registers primitive-shape rules for the price write command.</summary>
    public SetOfferPriceCommandValidator()
    {
        OfferFluentRules.RequireOfferId(this, x => x.OfferId);
        OfferFluentRules.RequireSellerPartyId(this, x => x.SellerPartyId);

        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode(OfferValidationCodes.AmountMin);

        RuleFor(x => x.Currency)
            .Must(currency => currency is null
                || string.IsNullOrWhiteSpace(currency)
                || currency.Trim().Length == 3)
            .WithErrorCode(OfferValidationCodes.CurrencyShape);

        OfferFluentRules.OptionalNonBlankShape(this, x => x.Market, OfferValidationCodes.MarketShape);
    }
}
