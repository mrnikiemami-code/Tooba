using FluentValidation;
using Tooba.Payment.Application.Commands.RetryUnpaidPayment;
using Tooba.Payment.Application.Validators;

namespace Tooba.Payment.Application.Validators.Storefront;

/// <summary>
/// Transport validation for <see cref="RetryUnpaidPaymentCommand"/>.
/// Same transport rules as the manual retry; unpaid eligibility stays in Application/Domain.
/// </summary>
public sealed class RetryUnpaidPaymentCommandValidator : AbstractValidator<RetryUnpaidPaymentCommand>
{
    /// <summary>Registers primitive-shape rules for the command.</summary>
    public RetryUnpaidPaymentCommandValidator()
    {
        PaymentFluentRules.RequireId(this, x => x.PaymentId, PaymentValidationCodes.PaymentIdRequired);
        PaymentFluentRules.RequireId(this, x => x.CartId, PaymentValidationCodes.CartIdRequired);
        PaymentFluentRules.OptionalNonBlankShape(this, x => x.GuestSecret, PaymentValidationCodes.GuestSecretShape);
        PaymentFluentRules.OptionalIdShape(this, x => x.AuthenticatedUserId, PaymentValidationCodes.AuthenticatedUserIdShape);
    }
}
