using FluentValidation;
using Tooba.AddressBook.Application.Commands.UpdateCustomerAddress;
using Tooba.AddressBook.Application.Validators;

namespace Tooba.AddressBook.Application.Validators.UpdateCustomerAddress;

/// <summary>
/// اعتبارسنجی شکل انتقال برای <see cref="UpdateCustomerAddressCommand"/> — فقط ورودی نامطمئن.
/// <c>ActorUserId</c> اعتبارسنجی نمی‌شود و <c>AddressId</c> فقط به‌عنوان شناسهٔ مسیر غیرخالی بررسی می‌شود؛
/// مالکیت/وجود و قواعد کسب‌وکار در Application/Domain می‌مانند.
/// </summary>
public sealed class UpdateCustomerAddressCommandValidator : AbstractValidator<UpdateCustomerAddressCommand>
{
    /// <summary>قواعد شکل اولیهٔ شناسهٔ مسیر و بدنهٔ نوشتن را ثبت می‌کند.</summary>
    public UpdateCustomerAddressCommandValidator()
    {
        AddressBookFluentRules.RequireId(this, x => x.AddressId, AddressBookValidationCodes.AddressIdRequired);
        AddressBookFluentRules.RequireNonBlank(this, x => x.Input.RecipientName, AddressBookValidationCodes.RecipientNameRequired);
        AddressBookFluentRules.RequireNonBlank(this, x => x.Input.ContactMobile, AddressBookValidationCodes.ContactMobileRequired);
        AddressBookFluentRules.RequireNonBlank(this, x => x.Input.CityName, AddressBookValidationCodes.CityNameRequired);
        AddressBookFluentRules.RequireNonBlank(this, x => x.Input.PostalCode, AddressBookValidationCodes.PostalCodeRequired);
        AddressBookFluentRules.RequireNonBlank(this, x => x.Input.PostalAddress, AddressBookValidationCodes.PostalAddressRequired);
    }
}
