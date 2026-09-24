using FluentValidation;
using Tooba.Payment.Application.Queries.GetStorefrontWalletQuote;
using Tooba.Payment.Application.Validators;

namespace Tooba.Payment.Application.Validators.Storefront;

/// <summary>
/// Transport validation for <see cref="GetStorefrontWalletQuoteQuery"/>.
/// Quote eligibility, ownership and wallet balance stay in Application/Domain.
/// </summary>
public sealed class GetStorefrontWalletQuoteQueryValidator : AbstractValidator<GetStorefrontWalletQuoteQuery>
{
    /// <summary>Registers primitive-shape rules for the query.</summary>
    public GetStorefrontWalletQuoteQueryValidator()
    {
        PaymentFluentRules.RequireId(this, x => x.CheckoutId, PaymentValidationCodes.CheckoutIdRequired);
        PaymentFluentRules.RequireId(this, x => x.CartId, PaymentValidationCodes.CartIdRequired);
        PaymentFluentRules.OptionalNonBlankShape(this, x => x.GuestSecret, PaymentValidationCodes.GuestSecretShape);
        PaymentFluentRules.OptionalIdShape(this, x => x.AuthenticatedUserId, PaymentValidationCodes.AuthenticatedUserIdShape);
    }
}
