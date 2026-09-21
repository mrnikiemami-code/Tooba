using Tooba.BuildingBlocks;

namespace Tooba.Pricing.Contracts;

/// <summary>
/// کد ارز ISO. از Locale یا Market حدس زده نمی‌شود و تومان نمایشی با ریال مخلوط نمی‌شود.
/// Owned public contract type (Tooba.Pricing.Contracts assembly).
/// </summary>
public readonly record struct CurrencyCode
{
    /// <summary>
    /// سه حرف بزرگ ISO مثل IRR یا USD.
    /// </summary>
    public string Value { get; }

    private CurrencyCode(string value) => Value = value;

    /// <summary>
    /// کد ارز را نرمال می‌کند. تومان/IRT ممنوع است چون منبع حقیقت ریال (IRR) است.
    /// </summary>
    public static CurrencyCode Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new SemanticException(new SemanticError(PricingErrorCodes.CurrencyInvalid));
        }

        var code = raw.Trim().ToUpperInvariant();
        if (code is "TMN" or "IRT" or "TOMAN")
        {
            throw new SemanticException(new SemanticError(PricingErrorCodes.CurrencyDisplayUnit));
        }

        if (code.Length != 3 || !code.All(char.IsAsciiLetter))
        {
            throw new SemanticException(new SemanticError(PricingErrorCodes.CurrencyInvalid));
        }

        return new CurrencyCode(code);
    }

    /// <summary>
    /// مقیاس اعشار برای گرد کردن. IRR بدون اعشار؛ بیشتر ارزها دو رقم.
    /// </summary>
    public int Scale => Value is "IRR" or "JPY" or "KRW" ? 0 : 2;

    /// <inheritdoc />
    public override string ToString() => Value;
}
