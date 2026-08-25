namespace Tooba.AddressBook.Application;

/// <summary>نمایهٔ خصوصی یک نشانی متعلق به Actor جاری؛ شناسهٔ مالک را به کلاینت برنمی‌گرداند.</summary>
public sealed record CustomerAddressRecord(
    Guid AddressId,
    string RecipientName,
    string ContactMobile,
    string Country,
    string? ProvinceName,
    string CityName,
    string PostalCode,
    string PostalAddress,
    string? BuildingUnit,
    string? Label,
    bool IsDefault,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>ورودی نوشتن دفترچه؛ OwnerUserId ندارد و هویت از Host می‌آید.</summary>
public sealed record CustomerAddressWrite(
    string RecipientName,
    string ContactMobile,
    string? Country,
    string? ProvinceName,
    string CityName,
    string PostalCode,
    string PostalAddress,
    string? BuildingUnit,
    string? Label,
    bool IsDefault);

/// <summary>
/// قرارداد کاربردی دفترچهٔ آدرس مشتری. تمام عملیات با Actor تأمین‌شده از Host محدود می‌شوند
/// و بدنهٔ درخواست اختیار مالکیت ندارد.
/// </summary>
public interface IAddressBookDirectory
{
    /// <summary>نشانی جدید را برای Actor می‌سازد و در صورت پیش‌فرض بودن، پیش‌فرض قبلی را اتمیک پاک می‌کند.</summary>
    Task<CustomerAddressRecord> CreateAsync(Guid actorUserId, CustomerAddressWrite input, CancellationToken cancellationToken);

    /// <summary>فهرست خصوصی Actor را با پیش‌فرض‌ها در ابتدا برمی‌گرداند.</summary>
    Task<IReadOnlyList<CustomerAddressRecord>> ListAsync(Guid actorUserId, CancellationToken cancellationToken);

    /// <summary>یک نشانی متعلق به Actor را برمی‌گرداند؛ ردیف غریبه یا غایب تهی است.</summary>
    Task<CustomerAddressRecord?> GetAsync(Guid actorUserId, Guid addressId, CancellationToken cancellationToken);

    /// <summary>نشانی متعلق به Actor را ویرایش می‌کند.</summary>
    Task<CustomerAddressRecord> UpdateAsync(Guid actorUserId, Guid addressId, CustomerAddressWrite input, CancellationToken cancellationToken);

    /// <summary>نشانی متعلق به Actor را حذف می‌کند؛ حذف پیش‌فرض جایگزین خودکار ندارد.</summary>
    Task DeleteAsync(Guid actorUserId, Guid addressId, CancellationToken cancellationToken);

    /// <summary>یک نشانی متعلق به Actor را پیش‌فرض می‌کند و پیش‌فرض قبلی را اتمیک برمی‌دارد.</summary>
    Task<CustomerAddressRecord> SetDefaultAsync(Guid actorUserId, Guid addressId, CancellationToken cancellationToken);

    /// <summary>تعداد نشانی‌های خصوصی Actor را برمی‌گرداند.</summary>
    Task<long> CountAsync(Guid actorUserId, CancellationToken cancellationToken);
}
