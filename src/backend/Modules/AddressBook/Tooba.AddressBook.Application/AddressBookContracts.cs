#pragma warning disable CS1591
using Tooba.AddressBook.Contracts;

namespace Tooba.AddressBook.Application;

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
    bool IsDefault,
    string FirstName = "",
    string LastName = "");

/// <summary>
/// قرارداد کاربردی دفترچهٔ آدرس مشتری. تمام عملیات با Actor تأمین‌شده از Host محدود می‌شوند
/// و بدنهٔ درخواست اختیار مالکیت ندارد.
/// </summary>
public interface IAddressBookDirectory : IAddressBookCheckoutLookup
{
    Task<CustomerAddressRecord> CreateAsync(Guid actorUserId, CustomerAddressWrite input, CancellationToken cancellationToken);
    Task<IReadOnlyList<CustomerAddressRecord>> ListAsync(Guid actorUserId, CancellationToken cancellationToken);
    Task<CustomerAddressRecord> UpdateAsync(Guid actorUserId, Guid addressId, CustomerAddressWrite input, CancellationToken cancellationToken);
    Task DeleteAsync(Guid actorUserId, Guid addressId, CancellationToken cancellationToken);
    Task<CustomerAddressRecord> SetDefaultAsync(Guid actorUserId, Guid addressId, CancellationToken cancellationToken);
    Task<long> CountAsync(Guid actorUserId, CancellationToken cancellationToken);
}

#pragma warning restore CS1591
