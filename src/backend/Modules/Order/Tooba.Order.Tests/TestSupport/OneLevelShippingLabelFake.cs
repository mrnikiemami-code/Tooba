using Tooba.Fulfillment.Contracts.Shipping;

namespace Tooba.Order.Tests;

/// <summary>
/// جایگزین تستی درز فقط-خواندنی برچسب یک‌سطحی Fulfillment.
/// </summary>
/// <remarks>
/// پیش‌فرض نگاشت خالی است، یعنی حالت دروازهٔ خاموش/کاتالوگ آماده‌نبودن: مصرف‌کننده باید برچسب
/// قدیمی خود را عیناً نگه دارد. فراخوانی‌ها برای اثبات «کد/زبان دقیقاً همان‌هایی که انتظار
/// می‌رود» ثبت می‌شوند.
/// </remarks>
internal sealed class OneLevelShippingLabelFake : IShippingOneLevelStorefrontLabelReader
{
    private readonly Dictionary<string, string> _names;

    private readonly Guid? _emptyForLanguage;

    /// <summary>نگاشت آزمایشی کد روش به نام نمایشی یک‌سطحی.</summary>
    /// <param name="names">نام‌های موجود؛ تهی یعنی حالت «دروازهٔ خاموش/کاتالوگ آماده نیست».</param>
    /// <param name="emptyForLanguage">
    /// زبانی که پاسخ برای آن همیشه خالی است؛ برای اثبات نبود ترجمهٔ زبان درخواستی (بدون خواندن
    /// برچسب زبان دیگر).
    /// </param>
    public OneLevelShippingLabelFake(
        IReadOnlyDictionary<string, string>? names = null,
        Guid? emptyForLanguage = null)
    {
        _names = names is null
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : new Dictionary<string, string>(names, StringComparer.Ordinal);
        _emptyForLanguage = emptyForLanguage;
    }

    /// <summary>تعداد فراخوانی‌های درز.</summary>
    public int CallCount { get; private set; }

    /// <summary>آخرین زبان درخواستی.</summary>
    public Guid? LastLanguageId { get; private set; }

    /// <summary>آخرین کدهای درخواستی.</summary>
    public IReadOnlyCollection<string> LastMethodCodes { get; private set; } = [];

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<string, string>> ResolveDisplayNamesAsync(
        IReadOnlyCollection<string> methodCodes,
        Guid languageId,
        CancellationToken cancellationToken)
    {
        CallCount++;
        LastLanguageId = languageId;
        LastMethodCodes = methodCodes.ToArray();

        var resolved = new Dictionary<string, string>(StringComparer.Ordinal);
        if (_emptyForLanguage is not null && _emptyForLanguage.Value == languageId)
        {
            return Task.FromResult<IReadOnlyDictionary<string, string>>(resolved);
        }

        foreach (var code in methodCodes)
        {
            if (_names.TryGetValue(code, out var name))
            {
                resolved[code] = name;
            }
        }

        return Task.FromResult<IReadOnlyDictionary<string, string>>(resolved);
    }
}
