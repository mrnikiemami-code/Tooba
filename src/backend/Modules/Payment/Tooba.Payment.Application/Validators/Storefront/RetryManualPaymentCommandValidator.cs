using FluentValidation;
using Tooba.Payment.Application.Commands.RetryManualPayment;
using Tooba.Payment.Application.Validators;

namespace Tooba.Payment.Application.Validators.Storefront;

/// <summary>
/// Transport validation for <see cref="RetryManualPaymentCommand"/>.
/// Retry eligibility, ownership and payment state stay in Application/Domain.
/// </summary>
public sealed class RetryManualPaymentCommandValidator : AbstractValidator<RetryManualPaymentCommand>
{
    /// <summary>Registers primitive-shape rules for the command.</summary>
    public RetryManualPaymentCommandValidator()
    {
        PaymentFluentRules.RequireId(this, x => x.PaymentId, PaymentValidationCodes.PaymentIdRequired);
        PaymentFluentRules.RequireId(this, x => x.CartId, PaymentValidationCodes.CartIdRequired);
        PaymentFluentRules.OptionalNonBlankShape(this, x => x.GuestSecret, PaymentValidationCodes.GuestSecretShape);
        PaymentFluentRules.OptionalIdShape(this, x => x.AuthenticatedUserId, PaymentValidationCodes.AuthenticatedUserIdShape);
    }
}
