using Tooba.Tax.Domain;

namespace Tooba.Tax.Application;

/// <summary>
/// ورودی محاسبه. نرخ را مشتری تزریق نمی‌کند؛ حوزه از Locale حدس زده نمی‌شود.
/// </summary>
public sealed record TaxCalculationRequest(
    Guid OfferId,
    string Jurisdiction,
    string Market,
    string Currency,
    decimal TaxExclusiveAmount,
    int Quantity,
    DateTimeOffset At,
    Guid? CustomerPartyId,
    bool AllowTrustedOverride,
    decimal? TrustedOverrideRate);

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
/// مرجع طبقه برای Catalog/Offer بدون مبلغ مالیات.
/// </summary>
public sealed record TaxCategoryReference(Guid CategoryId, string Code, string DisplayName);

/// <summary>
/// مرجع قاعده برای ادمین/آزمون. قانون کشور در کد نیست.
/// </summary>
public sealed record TaxRuleReference(
    Guid RuleId,
    string Jurisdiction,
    string Market,
    Guid CategoryId,
    TaxRuleKind Kind,
    decimal Rate,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveTo,
    TaxRuleStatus Status,
    int Specificity);

/// <summary>
/// نگهبان موردکاربرد Tax.
/// </summary>
public interface ITaxUseCaseGuard
{
    /// <summary>
    /// اجازهٔ نوشتن قاعده و طبقه را بررسی می‌کند.
    /// </summary>
    Task EnsureCanMutateAsync(CancellationToken cancellationToken);
}

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

/// <summary>
/// نوشتن پیکربندی مالیات. فاکتور B2B و درگاه پرداخت اینجا نیستند.
/// </summary>
public interface ITaxDirectory : ITaxCalculator
{
    /// <summary>
    /// طبقهٔ مات می‌سازد.
    /// </summary>
    Task<TaxCategoryReference> CreateCategoryAsync(string code, string displayName, CancellationToken cancellationToken);

    /// <summary>
    /// Offer را به طبقه وصل می‌کند؛ نرخ روی Offer ذخیره نمی‌شود.
    /// </summary>
    Task AssignOfferCategoryAsync(Guid offerId, Guid categoryId, CancellationToken cancellationToken);

    /// <summary>
    /// قاعدهٔ مؤثر به تاریخ می‌سازد.
    /// </summary>
    Task<TaxRuleReference> CreateRuleAsync(
        string jurisdiction,
        string market,
        Guid categoryId,
        TaxRuleKind kind,
        decimal rate,
        DateTimeOffset effectiveFrom,
        DateTimeOffset? effectiveTo,
        int specificity,
        TaxOverridePolicy overridePolicy,
        CancellationToken cancellationToken);

    /// <summary>
    /// قاعده را فعال می‌کند.
    /// </summary>
    Task ActivateRuleAsync(Guid ruleId, CancellationToken cancellationToken);

    /// <summary>
    /// نرخ قاعدهٔ درصدی را عوض می‌کند. تصویر سفارش قبلی را تغییر نمی‌دهد.
    /// </summary>
    Task ChangeRuleRateAsync(Guid ruleId, decimal rate, CancellationToken cancellationToken);
}
