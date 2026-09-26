using MediatR;
using Tooba.AddressBook.Application.Ports;
using Tooba.AddressBook.Contracts.Dtos;

namespace Tooba.AddressBook.Application.Commands.SetDefaultCustomerAddress;

/// <summary>
/// تعیین نشانی پیش‌فرض مشتری. <c>ActorUserId</c> از context سرور و <c>AddressId</c> از مسیر می‌آید؛
/// مالکیت/وجود و رفتار خطا بدون ترجمه در Application/Domain می‌ماند.
/// </summary>
public sealed record SetDefaultCustomerAddressCommand(Guid ActorUserId, Guid AddressId)
    : IRequest<CustomerAddressRecord>;

/// <summary>Handler تعیین پیش‌فرض؛ فقط از دایرکتوری ماژول استفاده می‌کند و DbContext را لمس نمی‌کند.</summary>
public sealed class SetDefaultCustomerAddressCommandHandler(IAddressBookDirectory addresses)
    : IRequestHandler<SetDefaultCustomerAddressCommand, CustomerAddressRecord>
{
    /// <inheritdoc />
    public Task<CustomerAddressRecord> Handle(
        SetDefaultCustomerAddressCommand request,
        CancellationToken cancellationToken)
        => addresses.SetDefaultAsync(request.ActorUserId, request.AddressId, cancellationToken);
}
