using FluentValidation;
using Tooba.AddressBook.Application.Commands.DeleteCustomerAddress;
using Tooba.AddressBook.Application.Validators;

namespace Tooba.AddressBook.Application.Validators.DeleteCustomerAddress;

/// <summary>
/// اعتبارسنجی شکل انتقال برای <see cref="DeleteCustomerAddressCommand"/> — فقط شناسهٔ مسیر.
/// <c>ActorUserId</c> اعتبارسنجی نمی‌شود و مالکیت/وجود در Application/Domain می‌ماند.
/// </summary>
public sealed class DeleteCustomerAddressCommandValidator : AbstractValidator<DeleteCustomerAddressCommand>
{
    /// <summary>قاعدهٔ شکل شناسهٔ مسیر را ثبت می‌کند.</summary>
    public DeleteCustomerAddressCommandValidator()
    {
        AddressBookFluentRules.RequireId(this, x => x.AddressId, AddressBookValidationCodes.AddressIdRequired);
    }
}
