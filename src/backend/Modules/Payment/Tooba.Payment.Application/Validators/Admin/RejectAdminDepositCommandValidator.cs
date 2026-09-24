using FluentValidation;
using Tooba.Payment.Application.Commands.RejectAdminDeposit;
using Tooba.Payment.Application.Validators;

namespace Tooba.Payment.Application.Validators.Admin;

/// <summary>
/// Transport validation for <see cref="RejectAdminDepositCommand"/>.
/// Deposit state and payment eligibility stay in Application/Domain.
/// </summary>
public sealed class RejectAdminDepositCommandValidator : AbstractValidator<RejectAdminDepositCommand>
{
    /// <summary>Registers primitive-shape rules for the command.</summary>
    public RejectAdminDepositCommandValidator()
    {
        PaymentFluentRules.RequireId(this, x => x.PaymentId, PaymentValidationCodes.PaymentIdRequired);
    }
}
