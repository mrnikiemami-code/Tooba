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
    public static MarketCode Parse(string raw) =>
        TryParse(raw, out var code, out var error)
            ? code
            : throw new SemanticException(error!);

    /// <summary>Normalizes a market code or returns the stable business error.</summary>
    public static bool TryParse(string raw, out MarketCode code, out SemanticError? error)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            code = default;
            error = new SemanticError(PricingErrorCodes.MarketInvalid);
            return false;
        }

        var normalized = raw.Trim().ToUpperInvariant();
        if (normalized.Length is < 2 or > 16 || !normalized.All(ch => char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_'))
        {
            code = default;
            error = new SemanticError(PricingErrorCodes.MarketInvalid);
            return false;
        }

        code = new MarketCode(normalized);
        error = null;
        return true;
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
