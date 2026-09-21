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
    public static CurrencyCode Parse(string raw) =>
        TryParse(raw, out var code, out var error)
            ? code
            : throw new SemanticException(error!);

    /// <summary>Normalizes a currency code or returns the stable business error.</summary>
    public static bool TryParse(string raw, out CurrencyCode code, out SemanticError? error)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            code = default;
            error = new SemanticError(PricingErrorCodes.CurrencyInvalid);
            return false;
        }

        var normalized = raw.Trim().ToUpperInvariant();
        if (normalized is "TMN" or "IRT" or "TOMAN")
        {
            code = default;
            error = new SemanticError(PricingErrorCodes.CurrencyDisplayUnit);
            return false;
        }

        if (normalized.Length != 3 || !normalized.All(char.IsAsciiLetter))
        {
            code = default;
            error = new SemanticError(PricingErrorCodes.CurrencyInvalid);
            return false;
        }

        code = new CurrencyCode(normalized);
        error = null;
        return true;
    }

    /// <summary>
    /// مقیاس اعشار برای گرد کردن. IRR بدون اعشار؛ بیشتر ارزها دو رقم.
    /// </summary>
    public int Scale => Value is "IRR" or "JPY" or "KRW" ? 0 : 2;

    /// <inheritdoc />
    public override string ToString() => Value;
}
