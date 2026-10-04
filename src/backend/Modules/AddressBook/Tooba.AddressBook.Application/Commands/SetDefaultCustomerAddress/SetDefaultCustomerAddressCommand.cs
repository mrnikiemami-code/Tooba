using MediatR;
using Tooba.AddressBook.Application.Composition;
using Tooba.AddressBook.Application.Ports;
using Tooba.AddressBook.Contracts.Dtos;
using Tooba.BuildingBlocks.Results;

namespace Tooba.AddressBook.Application.Commands.SetDefaultCustomerAddress;

/// <summary>
/// تعیین نشانی پیش‌فرض مشتری. <c>ActorUserId</c> از context سرور و <c>AddressId</c> از مسیر می‌آید؛
/// مالکیت/وجود و رفتار خطا بدون ترجمه در Application/Domain می‌ماند.
/// </summary>
public sealed record SetDefaultCustomerAddressCommand(Guid ActorUserId, Guid AddressId)
    : IRequest<Result<CustomerAddressRecord>>;

/// <summary>Handler تعیین پیش‌فرض؛ فقط از دایرکتوری ماژول استفاده می‌کند و DbContext را لمس نمی‌کند.</summary>
public sealed class SetDefaultCustomerAddressCommandHandler(IAddressBookDirectory addresses)
    : IRequestHandler<SetDefaultCustomerAddressCommand, Result<CustomerAddressRecord>>
{
    /// <inheritdoc />
    public Task<Result<CustomerAddressRecord>> Handle(
        SetDefaultCustomerAddressCommand request,
        CancellationToken cancellationToken)
        => AddressBookOperation.ExecuteAsync(
            () => addresses.SetDefaultAsync(request.ActorUserId, request.AddressId, cancellationToken));
}
