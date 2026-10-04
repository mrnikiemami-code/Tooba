using MediatR;
using Tooba.AddressBook.Application.Composition;
using Tooba.AddressBook.Application.Models;
using Tooba.AddressBook.Application.Ports;
using Tooba.AddressBook.Contracts.Dtos;
using Tooba.BuildingBlocks.Results;

namespace Tooba.AddressBook.Application.Commands.UpdateCustomerAddress;

/// <summary>
/// ویرایش نشانی مشتری. <c>ActorUserId</c> از context سرور و <c>AddressId</c> از مسیر می‌آید؛
/// مالکیت/وجود و قواعد کسب‌وکار در <c>CustomerAddress</c>/<c>AddressBookDirectory</c> می‌مانند.
/// </summary>
public sealed record UpdateCustomerAddressCommand(Guid ActorUserId, Guid AddressId, CustomerAddressWrite Input)
    : IRequest<Result<CustomerAddressRecord>>;

/// <summary>Handler ویرایش نشانی؛ فقط از دایرکتوری ماژول استفاده می‌کند و DbContext را لمس نمی‌کند.</summary>
public sealed class UpdateCustomerAddressCommandHandler(IAddressBookDirectory addresses)
    : IRequestHandler<UpdateCustomerAddressCommand, Result<CustomerAddressRecord>>
{
    /// <inheritdoc />
    public Task<Result<CustomerAddressRecord>> Handle(
        UpdateCustomerAddressCommand request,
        CancellationToken cancellationToken)
        => AddressBookOperation.ExecuteAsync(
            () => addresses.UpdateAsync(request.ActorUserId, request.AddressId, request.Input, cancellationToken));
}
