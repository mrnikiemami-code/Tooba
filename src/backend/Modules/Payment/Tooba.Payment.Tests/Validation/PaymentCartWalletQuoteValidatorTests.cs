using FluentValidation.TestHelper;
using Tooba.Payment.Application.Storefront.Queries;
using Tooba.Payment.Application.Storefront.Validators;
using Xunit;

namespace Tooba.Payment.Tests.Validation;

/// <summary>
/// TB-P0-CHECKOUT-WALLET-ENDTOEND-031 — دو سطح مالکیت نقل‌قول کیف‌پول با یک قرارداد transport:
/// سطح سفارش متعهد (CheckoutId اجباری) و سطح سبد پیش‌از‌سفارش (CheckoutId باید غایب باشد).
/// اهلیت/موجودی/مالکیت همچنان در Application/Domain است و اینجا فقط شکل ورودی بررسی می‌شود.
/// </summary>
public sealed class PaymentCartWalletQuoteValidatorTests
{
    private static readonly Guid Id = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void Cart_scoped_quote_requires_cart_and_forbids_checkout_id()
    {
        var validator = new GetStorefrontWalletQuoteQueryValidator();

        validator.TestValidate(new GetStorefrontWalletQuoteQuery(null, Id, null, null, CartScoped: true))
            .ShouldNotHaveAnyValidationErrors();

        validator.TestValidate(new GetStorefrontWalletQuoteQuery(null, Id, "guest", Id, CartScoped: true))
            .ShouldNotHaveAnyValidationErrors();

        validator.TestValidate(new GetStorefrontWalletQuoteQuery(null, Guid.Empty, null, Id, CartScoped: true))
            .ShouldHaveValidationErrorFor(x => x.CartId);

        validator.TestValidate(new GetStorefrontWalletQuoteQuery(Id, Id, null, Id, CartScoped: true))
            .ShouldHaveValidationErrorFor(x => x.CheckoutId);

        validator.TestValidate(new GetStorefrontWalletQuoteQuery(null, Id, "   ", Id, CartScoped: true))
            .ShouldHaveValidationErrorFor(x => x.GuestSecret);

        validator.TestValidate(new GetStorefrontWalletQuoteQuery(null, Id, null, Guid.Empty, CartScoped: true))
            .ShouldHaveValidationErrorFor(x => x.AuthenticatedUserId);
    }

    [Fact]
    public void Checkout_scoped_quote_still_requires_checkout_id()
    {
        var validator = new GetStorefrontWalletQuoteQueryValidator();

        validator.TestValidate(new GetStorefrontWalletQuoteQuery(null, Id, null, Id))
            .ShouldHaveValidationErrorFor(x => x.CheckoutId);

        validator.TestValidate(new GetStorefrontWalletQuoteQuery(Id, Id, null, Id))
            .ShouldNotHaveAnyValidationErrors();
    }
}
