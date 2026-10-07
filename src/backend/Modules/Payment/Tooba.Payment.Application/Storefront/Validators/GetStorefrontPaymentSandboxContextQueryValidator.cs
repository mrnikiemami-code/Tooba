using FluentValidation;
using Tooba.Payment.Application.Storefront.Queries;
using Tooba.Payment.Application.Validators;

namespace Tooba.Payment.Application.Storefront.Validators;

/// <summary>
/// Transport validation for <see cref="GetStorefrontPaymentSandboxContextQuery"/>.
/// Same transport rules as the storefront payment read; sandbox eligibility stays in Application/Domain.
/// </summary>
public sealed class GetStorefrontPaymentSandboxContextQueryValidator
    : AbstractValidator<GetStorefrontPaymentSandboxContextQuery>
{
    /// <summary>Registers primitive-shape rules for the query.</summary>
    public GetStorefrontPaymentSandboxContextQueryValidator()
    {
        PaymentFluentRules.RequireId(this, x => x.PaymentId, PaymentValidationCodes.PaymentIdRequired);
        PaymentFluentRules.RequireId(this, x => x.CartId, PaymentValidationCodes.CartIdRequired);
        PaymentFluentRules.OptionalNonBlankShape(this, x => x.GuestSecret, PaymentValidationCodes.GuestSecretShape);
        PaymentFluentRules.OptionalIdShape(this, x => x.AuthenticatedUserId, PaymentValidationCodes.AuthenticatedUserIdShape);
    }
}
