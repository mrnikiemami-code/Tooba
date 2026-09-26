using MediatR;
using Tooba.AddressBook.Application.Customer.Models;
using Tooba.AddressBook.Application.Customer.Ports;
using Tooba.AddressBook.Contracts.Customer;

namespace Tooba.AddressBook.Application.Customer.Create;

/// <summary>
/// ایجاد نشانی مشتری. <c>ActorUserId</c> از context سرور می‌آید و payload درخواست نیست؛
/// قواعد کسب‌وکار مالکیت/شکل نهایی در <c>CustomerAddress</c>/<c>AddressBookDirectory</c> می‌مانند.
/// </summary>
public sealed record CreateCustomerAddressCommand(Guid ActorUserId, CustomerAddressWrite Input)
    : IRequest<CustomerAddressRecord>;

/// <summary>Handler ایجاد نشانی؛ فقط از دایرکتوری ماژول استفاده می‌کند و DbContext را لمس نمی‌کند.</summary>
public sealed class CreateCustomerAddressCommandHandler(IAddressBookDirectory addresses)
    : IRequestHandler<CreateCustomerAddressCommand, CustomerAddressRecord>
{
    /// <inheritdoc />
    public Task<CustomerAddressRecord> Handle(
        CreateCustomerAddressCommand request,
        CancellationToken cancellationToken)
        => addresses.CreateAsync(request.ActorUserId, request.Input, cancellationToken);
}
