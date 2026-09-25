using FluentValidation;
using Tooba.Settlement.Application.Commands.RequestSellerPayout;

namespace Tooba.Settlement.Application.Validators.Seller;

/// <summary>
/// Transport validation for <see cref="RequestSellerPayoutCommand"/>.
/// Only the untrusted request body shape is checked. <c>SellerPartyId</c> and <c>ActorUserId</c>
/// come from the trusted seller authorization boundary and are deliberately not validated here;
/// available balance, payout eligibility, seller existence, idempotency uniqueness and payout
/// state/business policy stay in Application/Domain.
/// </summary>
public sealed class RequestSellerPayoutCommandValidator : AbstractValidator<RequestSellerPayoutCommand>
{
    /// <summary>Registers primitive-shape rules for the untrusted payout body.</summary>
    public RequestSellerPayoutCommandValidator()
    {
        RuleFor(x => x.Amount)
            .Must(amount => amount > 0)
            .WithErrorCode(SettlementValidationCodes.PayoutAmountPositive);

        RuleFor(x => x.IdempotencyKey)
            .Must(key => !string.IsNullOrWhiteSpace(key))
            .WithErrorCode(SettlementValidationCodes.IdempotencyKeyShape);
    }
}
