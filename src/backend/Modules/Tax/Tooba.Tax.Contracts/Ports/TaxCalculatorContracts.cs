using Tooba.BuildingBlocks;

namespace Tooba.Tax.Contracts;

/// <summary>
/// ورودی محاسبه. نرخ را مشتری تزریق نمی‌کند؛ حوزه از Locale حدس زده نمی‌شود.
/// </summary>
public sealed record TaxCalculationRequest(
    Guid OfferId,
    string Jurisdiction,
    string Market,
    string Currency,
    decimal TaxExclusiveAmount,
    decimal Quantity,
    DateTimeOffset At,
    Guid? CustomerPartyId,
    bool AllowTrustedOverride,
    decimal? TrustedOverrideRate,
    QuantityRoundingMode RoundingMode = QuantityRoundingMode.Nearest);

/// <summary>
/// خروجی صریح محاسبه. معافیت با نرخ صفر و نبودن قاعده یکی نیست.
/// </summary>
public sealed record TaxCalculationResult(
    TaxOutcome Outcome,
    decimal TaxExclusiveAmount,
    decimal TaxRate,
    decimal TaxAmount,
    decimal TaxInclusiveAmount,
    string Currency,
    Guid? RuleId,
    Guid? CategoryId,
    DateTimeOffset CalculatedAt);

/// <summary>
/// محاسبهٔ مالیات برای Checkout و نمایش تخمینی. Pricing را بازنویسی نمی‌کند.
/// </summary>
public interface ITaxCalculator
{
    /// <summary>
    /// مالیات را برای یک خط تجاری حساب می‌کند. شکست‌خورده صفر ساختگی برنمی‌گرداند مگر outcome صریح معاف/نرخ‌صفر.
    /// </summary>
    Task<TaxCalculationResult> CalculateAsync(TaxCalculationRequest request, CancellationToken cancellationToken);
}
