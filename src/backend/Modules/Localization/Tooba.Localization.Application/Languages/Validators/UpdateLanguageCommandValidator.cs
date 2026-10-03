using FluentValidation;
using Tooba.Localization.Application.Languages.Commands;
using Tooba.Localization.Contracts.Errors;

namespace Tooba.Localization.Application.Languages.Validators;

/// <summary>Transport-shape validation for <see cref="UpdateLanguageCommand"/>.</summary>
public sealed class UpdateLanguageCommandValidator : AbstractValidator<UpdateLanguageCommand>
{
    /// <summary>Registers primitive-shape rules.</summary>
    public UpdateLanguageCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithErrorCode(LanguageErrorCodes.InvalidCode);
        RuleFor(x => x.DisplayName).NotEmpty().WithErrorCode(LanguageErrorCodes.InvalidDisplayName);
        RuleFor(x => x.NativeName).NotEmpty().WithErrorCode(LanguageErrorCodes.InvalidNativeName);
        RuleFor(x => x.Direction).NotEmpty().WithErrorCode(LanguageErrorCodes.InvalidDirection);
        RuleFor(x => x.Culture).NotEmpty().WithErrorCode(LanguageErrorCodes.InvalidCulture);
        RuleFor(x => x.CalendarDisplay).NotEmpty().WithErrorCode(LanguageErrorCodes.InvalidCalendar);
    }
}
