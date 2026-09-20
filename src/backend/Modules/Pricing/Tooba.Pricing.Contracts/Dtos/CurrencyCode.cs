namespace Tooba.Pricing.Domain;

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
            throw new InvalidOperationException("کد ارز خالی نیست؛ از Locale هم استنباط نمی‌شود.");
        }

        var code = raw.Trim().ToUpperInvariant();
        if (code is "TMN" or "IRT" or "TOMAN")
        {
            throw new InvalidOperationException("تومان واحد نمایش است نه ارز ذخیره‌شده؛ مبلغ نوشته‌شده باید IRR باشد.");
        }

        if (code.Length != 3 || !code.All(char.IsAsciiLetter))
        {
            throw new InvalidOperationException("ارز باید کد سه حرفی ISO باشد نه زبان UI.");
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
