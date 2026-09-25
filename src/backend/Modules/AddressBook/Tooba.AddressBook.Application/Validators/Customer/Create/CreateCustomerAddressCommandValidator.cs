using FluentValidation;
using Tooba.AddressBook.Application.Customer.Create;
using Tooba.AddressBook.Application.Validators;

namespace Tooba.AddressBook.Application.Validators.Customer.Create;

/// <summary>
/// اعتبارسنجی شکل انتقال برای <see cref="CreateCustomerAddressCommand"/> — فقط ورودی نامطمئن.
/// <c>ActorUserId</c> از مرز اعتماد سرور می‌آید و اعتبارسنجی نمی‌شود؛
/// طول/قالب و همهٔ قواعد کسب‌وکار در Application/Domain می‌مانند.
/// </summary>
public sealed class CreateCustomerAddressCommandValidator : AbstractValidator<CreateCustomerAddressCommand>
{
    /// <summary>قواعد شکل اولیهٔ بدنهٔ نوشتن را ثبت می‌کند.</summary>
    public CreateCustomerAddressCommandValidator()
    {
        AddressBookFluentRules.RequireNonBlank(this, x => x.Input.RecipientName, AddressBookValidationCodes.RecipientNameRequired);
        AddressBookFluentRules.RequireNonBlank(this, x => x.Input.ContactMobile, AddressBookValidationCodes.ContactMobileRequired);
        AddressBookFluentRules.RequireNonBlank(this, x => x.Input.CityName, AddressBookValidationCodes.CityNameRequired);
        AddressBookFluentRules.RequireNonBlank(this, x => x.Input.PostalCode, AddressBookValidationCodes.PostalCodeRequired);
        AddressBookFluentRules.RequireNonBlank(this, x => x.Input.PostalAddress, AddressBookValidationCodes.PostalAddressRequired);
    }
}
