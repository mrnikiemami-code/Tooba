using Tooba.Promotion.Domain.Aggregates;

namespace Tooba.Promotion.Application.Promotions.Ports;

/// <summary>
/// تصویر (snapshot) کاندیدهای Promotion در دامنهٔ یک scope.
/// مالک خواندن جدول promotion همین کلاس است؛ تولیدکنندهٔ داده از بیرون تزریق می‌شود تا ماژول Promotion
/// تنها مصرف‌کنندهٔ Contracts باقی بماند و هیچ entity قابل‌تغییر از مرز ماژول بیرون نرود.
/// </summary>
public sealed class PromotionCandidateSnapshot : IPromotionCandidateSource
{
    private readonly Func<CancellationToken, Task<IReadOnlyList<PromotionDefinition>>> _load;
    private Task<IReadOnlyList<PromotionDefinition>>? _current;

    /// <summary>
    /// تصویر را به تولیدکنندهٔ دادهٔ مالک وصل می‌کند.
    /// </summary>
    public PromotionCandidateSnapshot(Func<CancellationToken, Task<IReadOnlyList<PromotionDefinition>>> load)
    {
        ArgumentNullException.ThrowIfNull(load);
        _load = load;
    }

    /// <summary>تعداد خواندن‌های واقعی انجام‌شده؛ فقط برای سنجش رفتار خواندن یک‌باره.</summary>
    public int LoadCount { get; private set; }

    /// <inheritdoc />
    public Task<IReadOnlyList<PromotionDefinition>> GetAsync(CancellationToken cancellationToken) =>
        _current ??= LoadAsync(cancellationToken);

    /// <inheritdoc />
    public void Invalidate()
    {
        _current = null;
    }

    private async Task<IReadOnlyList<PromotionDefinition>> LoadAsync(CancellationToken cancellationToken)
    {
        LoadCount++;
        return await _load(cancellationToken);
    }
}
