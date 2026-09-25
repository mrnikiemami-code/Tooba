using FluentValidation;
using Tooba.Settlement.Application.Commands.ProcessAdminPayout;

namespace Tooba.Settlement.Application.Validators.Admin;

/// <summary>
/// Transport validation for <see cref="ProcessAdminPayoutCommand"/>.
/// Only the untrusted route identifier is checked. <c>ActorUserId</c> is supplied by the trusted
/// admin authorization boundary and is deliberately not validated here; payout existence, state
/// and eligibility stay in Application/Domain.
/// </summary>
public sealed class ProcessAdminPayoutCommandValidator : AbstractValidator<ProcessAdminPayoutCommand>
{
    /// <summary>Registers primitive-shape rules for the route identifier.</summary>
    public ProcessAdminPayoutCommandValidator()
    {
        RuleFor(x => x.PayoutRequestId)
            .NotEmpty()
            .WithErrorCode(SettlementValidationCodes.PayoutRequestIdRequired);
    }
}
