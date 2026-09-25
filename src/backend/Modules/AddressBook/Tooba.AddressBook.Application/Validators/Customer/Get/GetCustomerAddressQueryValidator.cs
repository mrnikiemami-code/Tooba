using FluentValidation;
using Tooba.AddressBook.Application.Customer.Get;

namespace Tooba.AddressBook.Application.Validators.Customer.Get;

/// <summary>
/// اعتبارسنجی شکل انتقال برای <see cref="GetCustomerAddressQuery"/> — فقط شناسهٔ مسیر.
/// وجود/بیگانگی نشانی و مالکیت در Application/Domain می‌ماند؛
/// <c>ActorUserId</c> اعتماد سرور است و به‌عنوان payload نامعتبر اعتبارسنجی نمی‌شود.
/// </summary>
public sealed class GetCustomerAddressQueryValidator : AbstractValidator<GetCustomerAddressQuery>
{
    /// <summary>قاعدهٔ شکل شناسهٔ مسیر را ثبت می‌کند.</summary>
    public GetCustomerAddressQueryValidator()
    {
        RuleFor(x => x.AddressId).NotEmpty().WithErrorCode("customer.address.id_required");
    }
}
