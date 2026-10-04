using MediatR;
using Tooba.AddressBook.Application.Composition;
using Tooba.AddressBook.Application.Ports;
using Tooba.AddressBook.Contracts.Dtos;
using Tooba.AddressBook.Contracts.Errors;
using Tooba.BuildingBlocks.Results;

namespace Tooba.AddressBook.Application.Queries.GetCustomerAddress;

/// <summary>
/// یک نشانی مشتری جاری. Identity از context سرور می‌آید و payload درخواست نیست؛
/// نبود/بیگانگی نشانی به شکست پایدار <c>customer.address.missing</c> نگاشت می‌شود تا مرز HTTP
/// تصمیم کسب‌وکار نگیرد و وجود نشانی بیگانه هرگز افشا نشود.
/// </summary>
public sealed record GetCustomerAddressQuery(Guid ActorUserId, Guid AddressId)
    : IRequest<Result<CustomerAddressRecord>>;

/// <summary>Handler یک نشانی؛ فقط از دایرکتوری ماژول استفاده می‌کند و DbContext را لمس نمی‌کند.</summary>
public sealed class GetCustomerAddressQueryHandler(IAddressBookDirectory addresses)
    : IRequestHandler<GetCustomerAddressQuery, Result<CustomerAddressRecord>>
{
    /// <inheritdoc />
    public async Task<Result<CustomerAddressRecord>> Handle(
        GetCustomerAddressQuery request,
        CancellationToken cancellationToken)
    {
        var wrapped = await AddressBookOperation.ExecuteAsync(
            () => addresses.GetAsync(request.ActorUserId, request.AddressId, cancellationToken));
        if (wrapped.IsFailure)
        {
            return Result.Failure<CustomerAddressRecord>(wrapped.Errors);
        }

        return AddressBookOperation.NotFoundIfNull(wrapped.Value, AddressBookErrorCodes.AddressMissing);
    }
}
