using Tooba.UserPreference.Domain;
using DomainUserPreference = Tooba.UserPreference.Domain.UserPreference;

namespace Tooba.UserPreference.Application;

/// <summary>کمک‌های شکل کلید/locale برای مرز Application بدون افشای Domain به Endpoints.</summary>
public static class UserPreferenceShapes
{
    /// <summary>locale پیش‌فرض پاسخ خالی.</summary>
    public const string DefaultLocale = DomainUserPreference.LocaleFa;

    /// <summary>نرمال‌سازی کلید UI با همان قواعد Domain.</summary>
    public static string NormalizeUiKey(string? key) => UiPreference.NormalizeKey(key);
}
