using DomainUserPreference = Tooba.UserPreference.Domain.Aggregates.UserPreference;
using DomainUiPreference = Tooba.UserPreference.Domain.Aggregates.UiPreference;

namespace Tooba.UserPreference.Application.Models;

/// <summary>نمایهٔ ترجیح کاربر بدون شناسهٔ مالک در پاسخ API.</summary>
public sealed record UserPreferenceSnapshot(
    string Locale,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>ورودی نوشتن ترجیح؛ فقط locale.</summary>
public sealed record UserPreferenceWrite(string Locale);

/// <summary>نمایهٔ ترجیح کلیددار UI بدون شناسهٔ مالک در پاسخ.</summary>
public sealed record UiPreferenceSnapshot(
    string Key,
    string JsonPayload,
    DateTimeOffset UpdatedAt);

/// <summary>ورودی نوشتن ترجیح UI؛ فقط JSON متنی.</summary>
public sealed record UiPreferenceWrite(string JsonPayload);

/// <summary>کمک‌های شکل کلید/locale برای مرز Application بدون افشای Domain به Endpoints.</summary>
public static class UserPreferenceShapes
{
    /// <summary>locale پیش‌فرض پاسخ خالی.</summary>
    public const string DefaultLocale = DomainUserPreference.LocaleFa;

    /// <summary>نرمال‌سازی کلید UI با همان قواعد Domain.</summary>
    public static string NormalizeUiKey(string? key) => DomainUiPreference.NormalizeKey(key);
}
