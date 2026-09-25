using MediatR;
using Tooba.AddressBook.Contracts;

namespace Tooba.AddressBook.Application.Customer.Update;

/// <summary>
/// ویرایش نشانی مشتری. <c>ActorUserId</c> از context سرور و <c>AddressId</c> از مسیر می‌آید؛
/// مالکیت/وجود و قواعد کسب‌وکار در <c>CustomerAddress</c>/<c>AddressBookDirectory</c> می‌مانند.
/// </summary>
public sealed record UpdateCustomerAddressCommand(Guid ActorUserId, Guid AddressId, CustomerAddressWrite Input)
    : IRequest<CustomerAddressRecord>;

/// <summary>Handler ویرایش نشانی؛ فقط از دایرکتوری ماژول استفاده می‌کند و DbContext را لمس نمی‌کند.</summary>
public sealed class UpdateCustomerAddressCommandHandler(IAddressBookDirectory addresses)
    : IRequestHandler<UpdateCustomerAddressCommand, CustomerAddressRecord>
{
    /// <inheritdoc />
    public Task<CustomerAddressRecord> Handle(
        UpdateCustomerAddressCommand request,
        CancellationToken cancellationToken)
        => addresses.UpdateAsync(request.ActorUserId, request.AddressId, request.Input, cancellationToken);
}
