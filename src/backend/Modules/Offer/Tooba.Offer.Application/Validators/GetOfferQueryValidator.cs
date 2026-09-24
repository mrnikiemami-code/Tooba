using FluentValidation;
using Tooba.Offer.Application.Queries.GetOffer;

namespace Tooba.Offer.Application.Validators;

/// <summary>
/// Transport validation for <see cref="GetOfferQuery"/>.
/// Offer ownership and existence remain business validation in the handler.
/// </summary>
public sealed class GetOfferQueryValidator : AbstractValidator<GetOfferQuery>
{
    /// <summary>Registers primitive-shape rules for the query.</summary>
    public GetOfferQueryValidator()
    {
        OfferFluentRules.RequireOfferId(this, x => x.OfferId);
        OfferFluentRules.RequireSellerPartyId(this, x => x.SellerPartyId);
    }
}
