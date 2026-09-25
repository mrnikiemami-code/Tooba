using MediatR;

namespace Tooba.AddressBook.Application.Customer.Delete;

/// <summary>
/// حذف نشانی مشتری. <c>ActorUserId</c> از context سرور و <c>AddressId</c> از مسیر می‌آید؛
/// مالکیت/وجود و رفتار خطا بدون ترجمه در Application/Domain می‌ماند.
/// </summary>
public sealed record DeleteCustomerAddressCommand(Guid ActorUserId, Guid AddressId)
    : IRequest<Unit>;

/// <summary>Handler حذف نشانی؛ فقط از دایرکتوری ماژول استفاده می‌کند و DbContext را لمس نمی‌کند.</summary>
public sealed class DeleteCustomerAddressCommandHandler(IAddressBookDirectory addresses)
    : IRequestHandler<DeleteCustomerAddressCommand, Unit>
{
    /// <inheritdoc />
    public async Task<Unit> Handle(
        DeleteCustomerAddressCommand request,
        CancellationToken cancellationToken)
    {
        await addresses.DeleteAsync(request.ActorUserId, request.AddressId, cancellationToken);
        return Unit.Value;
    }
}
