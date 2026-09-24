using FluentValidation;
using Tooba.Payment.Application.Queries.GetStorefrontPayment;
using Tooba.Payment.Application.Validators;

namespace Tooba.Payment.Application.Validators.Storefront;

/// <summary>
/// Transport validation for <see cref="GetStorefrontPaymentQuery"/>.
/// Payment existence and ownership stay in Application/Domain.
/// </summary>
public sealed class GetStorefrontPaymentQueryValidator : AbstractValidator<GetStorefrontPaymentQuery>
{
    /// <summary>Registers primitive-shape rules for the query.</summary>
    public GetStorefrontPaymentQueryValidator()
    {
        PaymentFluentRules.RequireId(this, x => x.PaymentId, PaymentValidationCodes.PaymentIdRequired);
        PaymentFluentRules.RequireId(this, x => x.CartId, PaymentValidationCodes.CartIdRequired);
        PaymentFluentRules.OptionalNonBlankShape(this, x => x.GuestSecret, PaymentValidationCodes.GuestSecretShape);
        PaymentFluentRules.OptionalIdShape(this, x => x.AuthenticatedUserId, PaymentValidationCodes.AuthenticatedUserIdShape);
    }
}
