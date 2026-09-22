using Tooba.Identity.Contracts;
using Tooba.OperatorProfile.Contracts;

namespace Tooba.Order.Application.Admin.Completeness.Models;

/// <summary>
/// برچسب انسانی Actor برای UI؛ هیچ شناسهٔ فنی در متن نمایشی نیست.
/// </summary>
public readonly record struct AdminOrderActorLabel(
    string Kind,
    string DisplayName,
    string DisplayFa,
    string DisplayEn)
{
    /// <summary>کنش بدون Actor انسانی.</summary>
    public static AdminOrderActorLabel System() =>
        new("system", "سیستم", "توسط سیستم", "By system");

    /// <summary>کنش با Actor شناخته‌شده.</summary>
    public static AdminOrderActorLabel User(string displayName) =>
        new("user", displayName, $"توسط {displayName}", $"By {displayName}");

    /// <summary>کنش با Actor ثبت‌شده که نمایش قابل استفاده‌ای ندارد.</summary>
    public static AdminOrderActorLabel UnknownUser() =>
        new("user", "کاربر نامشخص", "توسط کاربر نامشخص", "By unknown user");
}

/// <summary>
/// نگاشت projection قرارداد Actor به برچسب نمایشی Order؛ بدون وابستگی به DbContext مالک.
/// </summary>
public static class AdminOrderActorLabels
{
    /// <summary>برچسب هر Actor را از projection نمایشی/تماس می‌سازد.</summary>
    public static IReadOnlyDictionary<Guid, AdminOrderActorLabel> Build(
        IReadOnlyCollection<Guid> actorUserIds,
        IReadOnlyDictionary<Guid, ActorDisplayProjection> displays,
        IReadOnlyDictionary<Guid, ActorContactProjection> contacts)
    {
        ArgumentNullException.ThrowIfNull(actorUserIds);
        ArgumentNullException.ThrowIfNull(displays);
        ArgumentNullException.ThrowIfNull(contacts);

        var map = new Dictionary<Guid, AdminOrderActorLabel>(actorUserIds.Count);
        foreach (var id in actorUserIds.Where(x => x != Guid.Empty).Distinct())
        {
            displays.TryGetValue(id, out var display);
            contacts.TryGetValue(id, out var contact);
            var name = FirstUsable(
                display?.DisplayName,
                JoinName(display?.FirstName, display?.LastName),
                display?.Email ?? contact?.Email,
                display?.Mobile ?? contact?.Mobile);
            map[id] = name is null ? AdminOrderActorLabel.UnknownUser() : AdminOrderActorLabel.User(name);
        }

        return map;
    }

    /// <summary>
    /// برچسب یک Actor را برمی‌گرداند: بدون شناسه = سیستم، شناسهٔ ناشناخته = کاربر نامشخص.
    /// </summary>
    public static AdminOrderActorLabel Resolve(
        Guid? actorUserId,
        IReadOnlyDictionary<Guid, AdminOrderActorLabel> labels)
    {
        ArgumentNullException.ThrowIfNull(labels);
        if (actorUserId is null || actorUserId == Guid.Empty)
        {
            return AdminOrderActorLabel.System();
        }

        return labels.TryGetValue(actorUserId.Value, out var found)
            ? found
            : AdminOrderActorLabel.UnknownUser();
    }

    private static string? FirstUsable(params string?[] values)
    {
        foreach (var value in values)
        {
            var usable = Usable(value);
            if (usable is not null)
            {
                return usable;
            }
        }

        return null;
    }

    private static string? Usable(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();

        // نام‌های خراب‌شدهٔ encoding که فقط '?' هستند نمایش معتبر نیستند.
        return trimmed.All(ch => ch == '?' || char.IsWhiteSpace(ch)) ? null : trimmed;
    }

    private static string? JoinName(string? first, string? last)
    {
        var parts = new[] { first, last }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .ToArray();
        return parts.Length == 0 ? null : string.Join(' ', parts);
    }
}
