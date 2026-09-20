using Tooba.BuildingBlocks;

namespace Tooba.Pricing.Domain;

/// <summary>
/// هویت بازار تجاری. زبان UI نیست و لزوماً یک ارز یکتا ندارد.
/// </summary>
public readonly record struct MarketCode
{
    /// <summary>
    /// کد پایدار بازار مثل IR یا UK.
    /// </summary>
    public string Value { get; }

    private MarketCode(string value) => Value = value;

    /// <summary>
    /// کد بازار را نرمال می‌کند. فقط ایران در schema قفل نمی‌شود.
    /// </summary>
    public static MarketCode Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new InvalidOperationException("بازار خالی نیست و با Locale یکی نیست.");
        }

        var code = raw.Trim().ToUpperInvariant();
        if (code.Length is < 2 or > 16 || !code.All(ch => char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_'))
        {
            throw new InvalidOperationException("کد بازار باید کوتاه و پایدار باشد نه نام زبان.");
        }

        return new MarketCode(code);
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
