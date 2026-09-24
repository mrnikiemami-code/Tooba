using FluentValidation;
using Tooba.Payment.Application.Commands.CompleteSandboxPayment;
using Tooba.Payment.Application.Validators;

namespace Tooba.Payment.Application.Validators.Storefront;

/// <summary>
/// Transport validation for <see cref="CompleteSandboxPaymentCommand"/>.
/// Attempt/payment state, ownership and outcome allow-listing stay in Application/Domain.
/// </summary>
public sealed class CompleteSandboxPaymentCommandValidator : AbstractValidator<CompleteSandboxPaymentCommand>
{
    /// <summary>Registers primitive-shape rules for the command.</summary>
    public CompleteSandboxPaymentCommandValidator()
    {
        PaymentFluentRules.RequireId(this, x => x.PaymentId, PaymentValidationCodes.PaymentIdRequired);
        PaymentFluentRules.RequireId(this, x => x.CartId, PaymentValidationCodes.CartIdRequired);
        PaymentFluentRules.RequireId(this, x => x.AttemptId, PaymentValidationCodes.AttemptIdRequired);
        PaymentFluentRules.RequireNonBlank(
            this, x => x.ProviderRequestReference, PaymentValidationCodes.ProviderRequestReferenceShape);
        PaymentFluentRules.RequireNonBlank(this, x => x.Outcome, PaymentValidationCodes.OutcomeShape);
        PaymentFluentRules.OptionalNonBlankShape(this, x => x.GuestSecret, PaymentValidationCodes.GuestSecretShape);
        PaymentFluentRules.OptionalIdShape(this, x => x.AuthenticatedUserId, PaymentValidationCodes.AuthenticatedUserIdShape);
    }
}
