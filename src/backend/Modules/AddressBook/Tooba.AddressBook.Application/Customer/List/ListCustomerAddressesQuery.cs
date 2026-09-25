using MediatR;
using Tooba.AddressBook.Contracts;

namespace Tooba.AddressBook.Application.Customer.List;

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
