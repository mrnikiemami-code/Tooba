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
