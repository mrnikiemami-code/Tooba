namespace Tooba.BuildingBlocks;

/// <summary>
/// یک حالت گرد کردن سراسری برای نرمال‌سازی مقدار کالا. گرد کردن پول جدا نیست.
/// </summary>
public enum QuantityRoundingMode
{
    /// <summary>رو به پایین.</summary>
    Floor = 0,

    /// <summary>رو به بالا.</summary>
    Ceiling = 1,

    /// <summary>نزدیک‌ترین مقدار (نیمه‌راه از صفر دور می‌شود).</summary>
    Nearest = 2,
}

/// <summary>
/// سیاست مؤثر مقدار کالا. امروز از Product می‌آید؛ Variant بعداً بدون تغییر مصرف‌کننده‌ها قابل افزودن است.
/// </summary>
public sealed record EffectiveQuantityPolicy(
    Guid ProductId,
    Guid UnitOfMeasureId,
    string UnitCode,
    string UnitDisplayName,
    string UnitShortName,
    int DecimalPlaces,
    decimal? Step,
    QuantityRoundingMode RoundingMode);

/// <summary>
/// نرمال‌سازی مقدار کالا با decimal؛ double ممنوع است.
/// </summary>
public interface IQuantityNormalizer
{
    /// <summary>مقدار ورودی را طبق سیاست و حالت گرد کردن سراسری نرمال می‌کند.</summary>
    decimal Normalize(decimal quantity, EffectiveQuantityPolicy policy);
}

/// <summary>پیاده‌سازی مرکزی نرمال‌سازی مقدار.</summary>
public sealed class QuantityNormalizer : IQuantityNormalizer
{
    /// <inheritdoc />
    public decimal Normalize(decimal quantity, EffectiveQuantityPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        if (quantity <= 0)
        {
            throw new InvalidOperationException("quantity.must_be_positive");
        }

        if (policy.DecimalPlaces is < 0 or > 6)
        {
            throw new InvalidOperationException("quantity.decimal_places.invalid");
        }

        if (policy.Step is { } step)
        {
            if (step <= 0)
            {
                throw new InvalidOperationException("quantity.step.invalid");
            }

            quantity = AlignToUnit(quantity, step, policy.RoundingMode);
        }

        var precision = DecimalPlacesUnit(policy.DecimalPlaces);
        return AlignToUnit(quantity, precision, policy.RoundingMode);
    }

    private static decimal DecimalPlacesUnit(int places)
    {
        var unit = 1m;
        for (var i = 0; i < places; i++)
        {
            unit /= 10m;
        }

        return unit;
    }

    private static decimal AlignToUnit(decimal value, decimal unit, QuantityRoundingMode mode)
    {
        var ratio = value / unit;
        var aligned = mode switch
        {
            QuantityRoundingMode.Floor => decimal.Floor(ratio),
            QuantityRoundingMode.Ceiling => decimal.Ceiling(ratio),
            QuantityRoundingMode.Nearest => decimal.Round(ratio, 0, MidpointRounding.AwayFromZero),
            _ => throw new InvalidOperationException("quantity.rounding.unsupported"),
        };
        return aligned * unit;
    }
}

/// <summary>قالب نمایش مقدار بدون صفرهای ذخیره‌سازی.</summary>
public static class QuantityDisplay
{
    /// <summary>۲ را ۲ نشان می‌دهد نه 2.000000؛ اعشار معنی‌دار می‌ماند.</summary>
    public static string Format(decimal quantity, int decimalPlaces)
    {
        var places = Math.Clamp(decimalPlaces, 0, 6);
        var rounded = decimal.Round(quantity, places, MidpointRounding.AwayFromZero);
        return rounded.ToString($"0.{new string('#', places)}", System.Globalization.CultureInfo.InvariantCulture);
    }
}
