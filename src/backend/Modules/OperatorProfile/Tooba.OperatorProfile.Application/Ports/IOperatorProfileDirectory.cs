using Tooba.OperatorProfile.Application.Models;

namespace Tooba.OperatorProfile.Application.Ports;

/// <summary>
/// قرارداد کاربردی پروفایل توصیفی اپراتور. تمام عملیات با Actor تأمین‌شده از Host محدود می‌شوند.
/// </summary>
public interface IOperatorProfileDirectory
{
    /// <summary>پروفایل Actor را برمی‌گرداند؛ در صورت نبود ردیف تهی است.</summary>
    Task<OperatorProfileSnapshot?> GetAsync(Guid actorUserId, CancellationToken cancellationToken);

    /// <summary>
    /// پروفایل چند Actor را در یک query محدود برمی‌گرداند (بدون N+1).
    /// کلیدهای بدون ردیف در دیکشنری نیستند.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, OperatorProfileSnapshot>> GetManyAsync(
        IReadOnlyCollection<Guid> actorUserIds,
        CancellationToken cancellationToken);

    /// <summary>پروفایل Actor را ایجاد یا به‌روز می‌کند.</summary>
    Task<OperatorProfileSnapshot> UpsertAsync(
        Guid actorUserId,
        OperatorProfileWrite input,
        CancellationToken cancellationToken);
}
