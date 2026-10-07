using FluentValidation;
using Tooba.Payment.Application.Admin.Commands;
using Tooba.Payment.Application.Validators;

namespace Tooba.Payment.Application.Admin.Validators;

/// <summary>
/// Transport validation for <see cref="ReconcileAdminPaymentCommand"/>.
/// Reconciliation eligibility, gateway availability and payment state stay in Application/Domain.
/// </summary>
public sealed class ReconcileAdminPaymentCommandValidator : AbstractValidator<ReconcileAdminPaymentCommand>
{
    /// <summary>Registers primitive-shape rules for the command.</summary>
    public ReconcileAdminPaymentCommandValidator()
    {
        PaymentFluentRules.RequireId(this, x => x.PaymentId, PaymentValidationCodes.PaymentIdRequired);
    }
}
