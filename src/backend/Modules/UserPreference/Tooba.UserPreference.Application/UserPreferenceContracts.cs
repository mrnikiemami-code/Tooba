namespace Tooba.UserPreference.Application;

/// <summary>نمایهٔ ترجیح کاربر بدون شناسهٔ مالک در پاسخ API.</summary>
public sealed record UserPreferenceSnapshot(
    string Locale,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>ورودی نوشتن ترجیح؛ فقط locale.</summary>
public sealed record UserPreferenceWrite(string Locale);

/// <summary>
/// قرارداد کاربردی ترجیح کاربر. تمام عملیات با Actor تأمین‌شده از Host محدود می‌شوند.
/// </summary>
public interface IUserPreferenceDirectory
{
    /// <summary>ترجیح Actor را برمی‌گرداند؛ در صورت نبود ردیف تهی است.</summary>
    Task<UserPreferenceSnapshot?> GetAsync(Guid actorUserId, CancellationToken cancellationToken);

    /// <summary>ترجیح Actor را ایجاد یا به‌روز می‌کند.</summary>
    Task<UserPreferenceSnapshot> UpsertAsync(
        Guid actorUserId,
        UserPreferenceWrite input,
        CancellationToken cancellationToken);
}

/// <summary>نمایهٔ ترجیح کلیددار UI بدون شناسهٔ مالک در پاسخ.</summary>
public sealed record UiPreferenceSnapshot(
    string Key,
    string JsonPayload,
    DateTimeOffset UpdatedAt);

/// <summary>ورودی نوشتن ترجیح UI؛ فقط JSON متنی.</summary>
public sealed record UiPreferenceWrite(string JsonPayload);

/// <summary>
/// قرارداد ترجیح‌های کلیددار UI. مالکیت فقط از Actor Host می‌آید.
/// </summary>
public interface IUiPreferenceDirectory
{
    /// <summary>ترجیح کلید را برای Actor برمی‌گرداند؛ نبود ردیف تهی است.</summary>
    Task<UiPreferenceSnapshot?> GetAsync(Guid actorUserId, string key, CancellationToken cancellationToken);

    /// <summary>ترجیح کلید را ایجاد یا به‌روز می‌کند.</summary>
    Task<UiPreferenceSnapshot> UpsertAsync(
        Guid actorUserId,
        string key,
        UiPreferenceWrite input,
        CancellationToken cancellationToken);
}
