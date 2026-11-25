using FluentValidation;
using Tooba.Offer.Application.Validation;

namespace Tooba.Offer.Application.Offers.Queries.GetOfferHistory;

/// <summary>Transport validation for <see cref="GetOfferHistoryQuery"/>; ownership stays in the handler.</summary>
public sealed class GetOfferHistoryQueryValidator : AbstractValidator<GetOfferHistoryQuery>
{
    /// <summary>Registers primitive-shape rules for the history query.</summary>
    public GetOfferHistoryQueryValidator()
    {
        OfferFluentRules.RequireOfferId(this, x => x.OfferId);
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithErrorCode(OfferValidationCodes.HistoryPageMin);
        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithErrorCode(OfferValidationCodes.HistoryPageSizeRange);
    }
}
