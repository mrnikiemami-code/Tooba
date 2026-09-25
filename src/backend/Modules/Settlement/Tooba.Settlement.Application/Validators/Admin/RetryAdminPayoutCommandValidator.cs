using FluentValidation;
using Tooba.Settlement.Application.Commands.RetryAdminPayout;

namespace Tooba.Settlement.Application.Validators.Admin;

/// <summary>
/// Transport validation for <see cref="RetryAdminPayoutCommand"/>.
/// Only the untrusted route identifier is checked. <c>ActorUserId</c> is supplied by the trusted
/// admin authorization boundary and is deliberately not validated here; retry eligibility, state
/// and business policy stay in Application/Domain.
/// </summary>
public sealed class RetryAdminPayoutCommandValidator : AbstractValidator<RetryAdminPayoutCommand>
{
    /// <summary>Registers primitive-shape rules for the route identifier.</summary>
    public RetryAdminPayoutCommandValidator()
    {
        RuleFor(x => x.PayoutRequestId)
            .NotEmpty()
            .WithErrorCode(SettlementValidationCodes.PayoutRequestIdRequired);
    }
}
