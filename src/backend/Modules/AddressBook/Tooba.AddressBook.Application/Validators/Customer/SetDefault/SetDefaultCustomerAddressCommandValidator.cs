using FluentValidation;
using Tooba.AddressBook.Application.Customer.SetDefault;
using Tooba.AddressBook.Application.Validators;

namespace Tooba.AddressBook.Application.Validators.Customer.SetDefault;

/// <summary>
/// اعتبارسنجی شکل انتقال برای <see cref="SetDefaultCustomerAddressCommand"/> — فقط شناسهٔ مسیر.
/// <c>ActorUserId</c> اعتبارسنجی نمی‌شود و مالکیت/وجود در Application/Domain می‌ماند.
/// </summary>
public sealed class SetDefaultCustomerAddressCommandValidator : AbstractValidator<SetDefaultCustomerAddressCommand>
{
    /// <summary>قاعدهٔ شکل شناسهٔ مسیر را ثبت می‌کند.</summary>
    public SetDefaultCustomerAddressCommandValidator()
    {
        AddressBookFluentRules.RequireId(this, x => x.AddressId, AddressBookValidationCodes.AddressIdRequired);
    }
}
