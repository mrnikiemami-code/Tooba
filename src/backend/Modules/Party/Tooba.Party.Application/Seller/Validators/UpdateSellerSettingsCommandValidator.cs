using FluentValidation;
using Tooba.Party.Domain;

namespace Tooba.Party.Application.Seller.Validators;

/// <summary>
/// اعتبارسنجی انتقالی ورودی به‌روزرسانی تنظیمات فروشنده.
/// فقط شکل/طول ورودی بررسی می‌شود؛ وجود Party، نوع Organization، مجوز و rules کسب‌وکار
/// در Application/Domain می‌مانند.
/// </summary>
public sealed class UpdateSellerSettingsCommandValidator
    : AbstractValidator<Commands.UpdateSellerSettingsCommand>
{
    /// <summary>قواعد شکل/طول ورودی را ثبت می‌کند.</summary>
    public UpdateSellerSettingsCommandValidator()
    {
        RuleFor(x => x.Input.DisplayName)
            .NotEmpty()
            .WithErrorCode(PartySellerSettingsValidationCodes.DisplayNameRequired)
            .MaximumLength(256)
            .WithErrorCode(PartySellerSettingsValidationCodes.DisplayNameLength);

        RuleFor(x => x.Input.LegalName)
            .MaximumLength(256)
            .WithErrorCode(PartySellerSettingsValidationCodes.LegalNameShape);

        RuleFor(x => x.Input.Description)
            .MaximumLength(BusinessParty.DescriptionMaxLength)
            .WithErrorCode(PartySellerSettingsValidationCodes.DescriptionShape);

        RuleFor(x => x.Input.SupportPhone)
            .MaximumLength(BusinessParty.SupportPhoneMaxLength)
            .WithErrorCode(PartySellerSettingsValidationCodes.SupportPhoneShape);

        RuleFor(x => x.Input.SupportEmail)
            .MaximumLength(BusinessParty.SupportEmailMaxLength)
            .WithErrorCode(PartySellerSettingsValidationCodes.SupportEmailShape);

        RuleFor(x => x.Input.AddressLine)
            .MaximumLength(BusinessParty.AddressLineMaxLength)
            .WithErrorCode(PartySellerSettingsValidationCodes.AddressLineShape);
    }
}
