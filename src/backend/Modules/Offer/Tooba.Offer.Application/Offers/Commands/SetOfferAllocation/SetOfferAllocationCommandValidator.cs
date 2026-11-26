using FluentValidation;

namespace Tooba.Offer.Application.Offers.Commands.SetOfferAllocation;

/// <summary>Transport validation for explicit offered quantity.</summary>
public sealed class SetOfferAllocationCommandValidator : AbstractValidator<SetOfferAllocationCommand>
{
    /// <summary>Requires an owned offer, non-negative quantity and explicit owner revision.</summary>
    public SetOfferAllocationCommandValidator()
    {
        RuleFor(x => x.OfferId).NotEmpty();
        RuleFor(x => x.SellerPartyId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ExpectedRevision).NotEmpty();
    }
}
