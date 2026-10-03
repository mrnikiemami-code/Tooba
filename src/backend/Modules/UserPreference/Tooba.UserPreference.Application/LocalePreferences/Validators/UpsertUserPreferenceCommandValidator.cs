using FluentValidation;
using Tooba.UserPreference.Application.LocalePreferences.Commands;

namespace Tooba.UserPreference.Application.LocalePreferences.Validators;

/// <summary>اعتبارسنجی شکل ورودی locale.</summary>
public sealed class UpsertUserPreferenceCommandValidator : AbstractValidator<UpsertUserPreferenceCommand>
{
    /// <summary>قواعد حمل‌ونقل؛ قواعد دامنه در Domain می‌مانند.</summary>
    public UpsertUserPreferenceCommandValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty().WithErrorCode("preference.validation.actor_required");
        RuleFor(x => x.Locale).NotEmpty().WithErrorCode("preference.validation.locale_required");
    }
}
