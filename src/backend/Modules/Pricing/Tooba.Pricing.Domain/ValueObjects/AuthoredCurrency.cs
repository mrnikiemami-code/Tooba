using Tooba.BuildingBlocks;

namespace Tooba.Pricing.Domain;

/// <summary>
/// ISO currency owned by the Pricing domain. Display units such as toman are rejected.
/// </summary>
public readonly record struct AuthoredCurrency
{
    /// <summary>Three-letter ISO code such as IRR or USD.</summary>
    public string Value { get; }

    private AuthoredCurrency(string value) => Value = value;

    /// <summary>Normalizes a currency code. Does not infer currency from locale.</summary>
    public static AuthoredCurrency Parse(string raw)
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

        return new AuthoredCurrency(code);
    }

    /// <summary>Decimal scale. IRR, JPY, and KRW have none; other currencies use two digits.</summary>
    public int Scale => Value is "IRR" or "JPY" or "KRW" ? 0 : 2;

    /// <inheritdoc />
    public override string ToString() => Value;
}
