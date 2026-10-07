using FluentValidation;
using Tooba.Payment.Application.Admin.Commands;
using Tooba.Payment.Application.Validators;

namespace Tooba.Payment.Application.Admin.Validators;

/// <summary>
/// Transport validation for <see cref="ConfirmAdminDepositCommand"/>.
/// Deposit state and payment eligibility stay in Application/Domain.
/// </summary>
public sealed class ConfirmAdminDepositCommandValidator : AbstractValidator<ConfirmAdminDepositCommand>
{
    /// <summary>Registers primitive-shape rules for the command.</summary>
    public ConfirmAdminDepositCommandValidator()
    {
        PaymentFluentRules.RequireId(this, x => x.PaymentId, PaymentValidationCodes.PaymentIdRequired);
    }
}
