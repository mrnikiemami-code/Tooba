namespace Tooba.CustomerProfile.Application;

/// <summary>نمایهٔ خصوصی پروفایل مشتری بدون شناسهٔ مالک در پاسخ API.</summary>
public sealed record CustomerProfileSnapshot(
    string FirstName,
    string LastName,
    string DisplayName,
    string? BirthDate,
    string? Bio,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>ورودی نوشتن پروفایل؛ email/mobile/password/nationalCode ندارد.</summary>
public sealed record CustomerProfileWrite(
    string DisplayName,
    string? FirstName,
    string? LastName,
    string? BirthDate,
    string? Bio);

/// <summary>
/// قرارداد کاربردی پروفایل توصیفی مشتری. تمام عملیات با Actor تأمین‌شده از Host محدود می‌شوند.
/// </summary>
public interface ICustomerProfileDirectory
{
    /// <summary>پروفایل Actor را برمی‌گرداند؛ در صورت نبود ردیف تهی است.</summary>
    Task<CustomerProfileSnapshot?> GetAsync(Guid actorUserId, CancellationToken cancellationToken);

    /// <summary>پروفایل Actor را ایجاد یا به‌روز می‌کند.</summary>
    Task<CustomerProfileSnapshot> UpsertAsync(
        Guid actorUserId,
        CustomerProfileWrite input,
        CancellationToken cancellationToken);
}
