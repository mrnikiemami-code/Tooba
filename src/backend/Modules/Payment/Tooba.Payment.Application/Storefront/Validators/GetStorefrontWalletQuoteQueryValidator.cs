using FluentValidation;
using Tooba.Payment.Application.Storefront.Queries;
using Tooba.Payment.Application.Validators;

namespace Tooba.Payment.Application.Storefront.Validators;

/// <summary>
/// اعتبارسنجی transport برای <see cref="GetStorefrontWalletQuoteQuery"/> در هر دو سطح مالکیت:
/// در سطح سفارش متعهد شناسهٔ سفارش اجباری است و در سطح سبد (پیش‌از‌سفارش) باید غایب باشد تا دو
/// قرارداد با هم قاطی نشوند. اهلیت کیف‌پول، مالکیت سبد/سفارش و موجودی همچنان در Application/Domain است.
/// </summary>
public sealed class GetStorefrontWalletQuoteQueryValidator : AbstractValidator<GetStorefrontWalletQuoteQuery>
{
    /// <summary>Registers primitive-shape rules for the query.</summary>
    public GetStorefrontWalletQuoteQueryValidator()
    {
        PaymentFluentRules.RequireId(this, x => x.CartId, PaymentValidationCodes.CartIdRequired);
        PaymentFluentRules.OptionalNonBlankShape(this, x => x.GuestSecret, PaymentValidationCodes.GuestSecretShape);
        PaymentFluentRules.OptionalIdShape(this, x => x.AuthenticatedUserId, PaymentValidationCodes.AuthenticatedUserIdShape);

        // سفارش متعهد: شناسه اجباری و غیرخالی.
        RuleFor(x => x.CheckoutId)
            .Must(value => value is { } id && id != Guid.Empty)
            .WithErrorCode(PaymentValidationCodes.CheckoutIdRequired)
            .When(x => !x.CartScoped);

        // مسیر سبد: شناسهٔ سفارش نباید همراه بیاید (مبلغ فقط از سبد می‌آید).
        RuleFor(x => x.CheckoutId)
            .Null()
            .WithErrorCode(PaymentValidationCodes.CheckoutIdRequired)
            .When(x => x.CartScoped);
    }
}
