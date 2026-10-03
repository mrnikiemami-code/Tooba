using Tooba.UserPreference.Application.Models;

namespace Tooba.UserPreference.Application.Ports;

/// <summary>
/// قرارداد ترجیح‌های کلیددار UI. مالکیت فقط از Actor سرور می‌آید.
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
