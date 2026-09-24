using FluentValidation;
using Tooba.Payment.Application.Commands.InitiateStorefrontPayment;
using Tooba.Payment.Application.Validators;

namespace Tooba.Payment.Application.Validators.Storefront;

/// <summary>
/// Transport validation for <see cref="InitiateStorefrontPaymentCommand"/>.
/// Provider selection policy, guest/authenticated ownership and payment eligibility stay in Application/Domain.
/// </summary>
public sealed class InitiateStorefrontPaymentCommandValidator : AbstractValidator<InitiateStorefrontPaymentCommand>
{
    /// <summary>Registers primitive-shape rules for the command.</summary>
    public InitiateStorefrontPaymentCommandValidator()
    {
        PaymentFluentRules.RequireId(this, x => x.CheckoutId, PaymentValidationCodes.CheckoutIdRequired);
        PaymentFluentRules.RequireId(this, x => x.CartId, PaymentValidationCodes.CartIdRequired);
        PaymentFluentRules.RequireNonBlank(this, x => x.IdempotencyKey, PaymentValidationCodes.IdempotencyKeyShape);
        PaymentFluentRules.OptionalNonBlankShape(this, x => x.ProviderCode, PaymentValidationCodes.ProviderCodeShape);
        PaymentFluentRules.OptionalNonBlankShape(this, x => x.GuestSecret, PaymentValidationCodes.GuestSecretShape);
        PaymentFluentRules.OptionalIdShape(this, x => x.AuthenticatedUserId, PaymentValidationCodes.AuthenticatedUserIdShape);
    }
}
