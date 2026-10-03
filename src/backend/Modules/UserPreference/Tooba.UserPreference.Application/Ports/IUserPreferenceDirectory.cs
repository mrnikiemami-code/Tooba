using Tooba.UserPreference.Application.Models;

namespace Tooba.UserPreference.Application.Ports;

/// <summary>
/// قرارداد کاربردی ترجیح کاربر. تمام عملیات با Actor تأمین‌شده از مرز اعتماد سرور محدود می‌شوند.
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
