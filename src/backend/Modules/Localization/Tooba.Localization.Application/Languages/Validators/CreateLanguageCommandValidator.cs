using FluentValidation;
using Tooba.Localization.Application.Languages.Commands;

namespace Tooba.Localization.Application.Languages.Validators;

/// <summary>Transport-shape validation for <see cref="CreateLanguageCommand"/>.</summary>
public sealed class CreateLanguageCommandValidator : AbstractValidator<CreateLanguageCommand>
{
    /// <summary>Registers primitive-shape rules.</summary>
    public CreateLanguageCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithErrorCode(LocalizationValidationCodes.LanguageCodeRequired);
        RuleFor(x => x.UrlPrefix).NotEmpty().WithErrorCode(LocalizationValidationCodes.LanguageUrlPrefixRequired);
        RuleFor(x => x.DisplayName).NotEmpty().WithErrorCode(LocalizationValidationCodes.LanguageDisplayNameRequired);
        RuleFor(x => x.NativeName).NotEmpty().WithErrorCode(LocalizationValidationCodes.LanguageNativeNameRequired);
        RuleFor(x => x.Direction).NotEmpty().WithErrorCode(LocalizationValidationCodes.LanguageDirectionRequired);
        RuleFor(x => x.Culture).NotEmpty().WithErrorCode(LocalizationValidationCodes.LanguageCultureRequired);
        RuleFor(x => x.CalendarDisplay).NotEmpty().WithErrorCode(LocalizationValidationCodes.LanguageCalendarRequired);
    }
}
