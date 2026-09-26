using MediatR;
using Tooba.AddressBook.Application.Ports;
using Tooba.AddressBook.Contracts.Dtos;

namespace Tooba.AddressBook.Application.Queries.ListCustomerAddresses;

/// <summary>
/// فهرست نشانی‌های مشتری جاری. Identity از context سرور می‌آید و payload درخواست نیست.
/// </summary>
public sealed record ListCustomerAddressesQuery(Guid ActorUserId)
    : IRequest<IReadOnlyList<CustomerAddressRecord>>;

/// <summary>Handler فهرست نشانی‌ها؛ فقط از دایرکتوری ماژول استفاده می‌کند و DbContext را لمس نمی‌کند.</summary>
public sealed class ListCustomerAddressesQueryHandler(IAddressBookDirectory addresses)
    : IRequestHandler<ListCustomerAddressesQuery, IReadOnlyList<CustomerAddressRecord>>
{
    /// <inheritdoc />
    public Task<IReadOnlyList<CustomerAddressRecord>> Handle(
        ListCustomerAddressesQuery request,
        CancellationToken cancellationToken)
        => addresses.ListAsync(request.ActorUserId, cancellationToken);
}
