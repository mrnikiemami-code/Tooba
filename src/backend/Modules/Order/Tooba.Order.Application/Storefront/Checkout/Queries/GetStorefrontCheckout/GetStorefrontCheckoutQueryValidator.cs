using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Storefront.Checkout.Queries.GetStorefrontCheckout;

public sealed class GetStorefrontCheckoutQueryValidator : AbstractValidator<GetStorefrontCheckoutQuery>
{
    public GetStorefrontCheckoutQueryValidator()
    {
        OrderFluentRules.RequireCheckoutId(this, x => x.CheckoutId);
        // TB240-R4: `CartId` فقط یک locator اختیاری است و سرویس عمداً آن را نادیده می‌گیرد؛ مالکیت واقعی از
        // طریق actor سرور (کاربر احراز‌شدهٔ OrderAccess) یا guest secret همان سبد متعهد سنجیده می‌شود.
        // بنابراین نبودِ آن برای خواندن مجازِ صاحب سفارش خطای اعتبارسنجی نیست.
    }
}