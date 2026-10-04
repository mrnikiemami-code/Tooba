using MediatR;
using Tooba.AddressBook.Application.Composition;
using Tooba.AddressBook.Application.Ports;
using Tooba.AddressBook.Contracts.Dtos;
using Tooba.BuildingBlocks.Results;

namespace Tooba.AddressBook.Application.Queries.ListCustomerAddresses;

/// <summary>
/// فهرست نشانی‌های مشتری جاری. Identity از context سرور می‌آید و payload درخواست نیست.
/// </summary>
public sealed record ListCustomerAddressesQuery(Guid ActorUserId)
    : IRequest<Result<IReadOnlyList<CustomerAddressRecord>>>;

/// <summary>Handler فهرست نشانی‌ها؛ فقط از دایرکتوری ماژول استفاده می‌کند و DbContext را لمس نمی‌کند.</summary>
public sealed class ListCustomerAddressesQueryHandler(IAddressBookDirectory addresses)
    : IRequestHandler<ListCustomerAddressesQuery, Result<IReadOnlyList<CustomerAddressRecord>>>
{
    /// <inheritdoc />
    public Task<Result<IReadOnlyList<CustomerAddressRecord>>> Handle(
        ListCustomerAddressesQuery request,
        CancellationToken cancellationToken)
        => AddressBookOperation.ExecuteAsync(
            () => addresses.ListAsync(request.ActorUserId, cancellationToken));
}
