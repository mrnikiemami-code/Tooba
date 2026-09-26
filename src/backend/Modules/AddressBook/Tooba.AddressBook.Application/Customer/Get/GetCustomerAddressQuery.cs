using MediatR;
using Tooba.AddressBook.Application.Customer.Ports;
using Tooba.AddressBook.Contracts.Customer;

namespace Tooba.AddressBook.Application.Customer.Get;

/// <summary>
/// یک نشانی مشتری جاری. Identity از context سرور می‌آید و payload درخواست نیست؛
/// نبود/بیگانگی نشانی با null بیان می‌شود و نگاشت وضعیت HTTP در لایهٔ ارائه است.
/// </summary>
public sealed record GetCustomerAddressQuery(Guid ActorUserId, Guid AddressId)
    : IRequest<CustomerAddressRecord?>;

/// <summary>Handler یک نشانی؛ فقط از دایرکتوری ماژول استفاده می‌کند و DbContext را لمس نمی‌کند.</summary>
public sealed class GetCustomerAddressQueryHandler(IAddressBookDirectory addresses)
    : IRequestHandler<GetCustomerAddressQuery, CustomerAddressRecord?>
{
    /// <inheritdoc />
    public Task<CustomerAddressRecord?> Handle(
        GetCustomerAddressQuery request,
        CancellationToken cancellationToken)
        => addresses.GetAsync(request.ActorUserId, request.AddressId, cancellationToken);
}
