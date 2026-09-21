using Tooba.BuildingBlocks;

namespace Tooba.Pricing.Domain;

/// <summary>
/// مبلغ نوشته‌شده با ارز صریح. ممیز شناور نیست و مالیات داخل مبلغ نیست.
/// </summary>
public readonly record struct Money
{
    /// <summary>
    /// مقدار پس از گرد کردن با AwayFromZero مطابق مقیاس ارز.
    /// </summary>
    public decimal Amount { get; }

    /// <summary>
    /// ارز مبلغ نوشته‌شده. تبدیل FX اینجا ذخیره نمی‌شود.
    /// </summary>
    public AuthoredCurrency Currency { get; }

    private Money(decimal amount, AuthoredCurrency currency)
    {
        Amount = amount;
        Currency = currency;
    }

    /// <summary>
    /// Money می‌سازد و به مقیاس ارز گرد می‌کند. نرخ ارز خارجی را به‌جای مبلغ نوشته‌شده نمی‌گذارد.
    /// </summary>
    public static Money Create(decimal amount, string currencyCode)
    {
        if (amount < 0)
        {
            throw new SemanticException(new SemanticError(PricingErrorCodes.AmountInvalid));
        }

        var currency = AuthoredCurrency.Parse(currencyCode);
        var rounded = decimal.Round(amount, currency.Scale, MidpointRounding.AwayFromZero);
        return new Money(rounded, currency);
    }

    /// <summary>
    /// مبلغ بدون مالیات است؛ VAT جدا محاسبه می‌شود.
    /// </summary>
    public bool IsTaxExclusive => true;
}
