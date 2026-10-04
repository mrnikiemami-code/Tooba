using MediatR;
using Tooba.AddressBook.Application.Composition;
using Tooba.AddressBook.Application.Models;
using Tooba.AddressBook.Application.Ports;
using Tooba.AddressBook.Contracts.Dtos;
using Tooba.BuildingBlocks.Results;

namespace Tooba.AddressBook.Application.Addresses.Commands;

/// <summary>
/// ایجاد نشانی مشتری. <c>ActorUserId</c> از context سرور می‌آید و payload درخواست نیست؛
/// قواعد کسب‌وکار مالکیت/شکل نهایی در <c>CustomerAddress</c>/<c>AddressBookDirectory</c> می‌مانند.
/// </summary>
public sealed record CreateCustomerAddressCommand(Guid ActorUserId, CustomerAddressWrite Input)
    : IRequest<Result<CustomerAddressRecord>>;

/// <summary>Handler ایجاد نشانی؛ فقط از دایرکتوری ماژول استفاده می‌کند و DbContext را لمس نمی‌کند.</summary>
public sealed class CreateCustomerAddressCommandHandler(IAddressBookDirectory addresses)
    : IRequestHandler<CreateCustomerAddressCommand, Result<CustomerAddressRecord>>
{
    /// <inheritdoc />
    public Task<Result<CustomerAddressRecord>> Handle(
        CreateCustomerAddressCommand request,
        CancellationToken cancellationToken)
        => AddressBookOperation.ExecuteAsync(
            () => addresses.CreateAsync(request.ActorUserId, request.Input, cancellationToken));
}
