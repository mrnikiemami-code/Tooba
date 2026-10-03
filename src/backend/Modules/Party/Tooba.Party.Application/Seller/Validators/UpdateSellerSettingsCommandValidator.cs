using FluentValidation;
using Tooba.Party.Application.Seller.Commands;
using Tooba.Party.Contracts.Errors;
using Tooba.Party.Domain.Aggregates;

namespace Tooba.Party.Application.Seller.Validators;

/// <summary>
/// اعتبارسنجی انتقالی ورودی به‌روزرسانی تنظیمات فروشنده.
/// فقط شکل/طول ورودی بررسی می‌شود؛ وجود Party، نوع Organization، مجوز و rules کسب‌وکار
/// در Application/Domain می‌مانند.
/// </summary>
public sealed class UpdateSellerSettingsCommandValidator
    : AbstractValidator<UpdateSellerSettingsCommand>
{
    /// <summary>قواعد شکل/طول ورودی را ثبت می‌کند.</summary>
    public UpdateSellerSettingsCommandValidator()
    {
        RuleFor(x => x.Input.DisplayName)
            .NotEmpty()
            .WithErrorCode(PartyErrorCodes.DisplayNameRequired)
            .MaximumLength(256)
            .WithErrorCode(PartyErrorCodes.DisplayNameLength);

        RuleFor(x => x.Input.LegalName)
            .MaximumLength(256)
            .WithErrorCode(PartyErrorCodes.LegalNameShape);

        RuleFor(x => x.Input.Description)
            .MaximumLength(BusinessParty.DescriptionMaxLength)
            .WithErrorCode(PartyErrorCodes.DescriptionShape);

        RuleFor(x => x.Input.SupportPhone)
            .MaximumLength(BusinessParty.SupportPhoneMaxLength)
            .WithErrorCode(PartyErrorCodes.SupportPhoneShape);

        RuleFor(x => x.Input.SupportEmail)
            .MaximumLength(BusinessParty.SupportEmailMaxLength)
            .WithErrorCode(PartyErrorCodes.SupportEmailShape);

        RuleFor(x => x.Input.AddressLine)
            .MaximumLength(BusinessParty.AddressLineMaxLength)
            .WithErrorCode(PartyErrorCodes.AddressLineShape);
    }
}
