namespace Tooba.OperatorProfile.Application;

/// <summary>نمایهٔ خصوصی پروفایل اپراتور بدون شناسهٔ مالک در پاسخ API.</summary>
public sealed record OperatorProfileSnapshot(
    string FirstName,
    string LastName,
    string DisplayName,
    string? Bio,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>ورودی نوشتن پروفایل اپراتور؛ تنظیمات سراسری platform ندارد.</summary>
public sealed record OperatorProfileWrite(
    string DisplayName,
    string? FirstName,
    string? LastName,
    string? Bio);

/// <summary>
/// قرارداد کاربردی پروفایل توصیفی اپراتور. تمام عملیات با Actor تأمین‌شده از Host محدود می‌شوند.
/// </summary>
public interface IOperatorProfileDirectory
{
    /// <summary>پروفایل Actor را برمی‌گرداند؛ در صورت نبود ردیف تهی است.</summary>
    Task<OperatorProfileSnapshot?> GetAsync(Guid actorUserId, CancellationToken cancellationToken);

    /// <summary>پروفایل Actor را ایجاد یا به‌روز می‌کند.</summary>
    Task<OperatorProfileSnapshot> UpsertAsync(
        Guid actorUserId,
        OperatorProfileWrite input,
        CancellationToken cancellationToken);
}
