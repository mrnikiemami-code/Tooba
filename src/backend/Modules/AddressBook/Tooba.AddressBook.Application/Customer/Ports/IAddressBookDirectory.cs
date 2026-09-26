using Tooba.AddressBook.Application.Customer.Models;
using Tooba.AddressBook.Contracts.Customer;

namespace Tooba.AddressBook.Application.Customer.Ports;

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
