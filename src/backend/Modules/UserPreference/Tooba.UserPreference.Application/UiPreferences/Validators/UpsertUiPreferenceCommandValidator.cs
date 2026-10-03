using FluentValidation;
using Tooba.UserPreference.Application.UiPreferences.Commands;

namespace Tooba.UserPreference.Application.UiPreferences.Validators;

/// <summary>اعتبارسنجی شکل ورودی UI preference.</summary>
public sealed class UpsertUiPreferenceCommandValidator : AbstractValidator<UpsertUiPreferenceCommand>
{
    /// <summary>قواعد حمل‌ونقل؛ قواعد دامنه در Domain می‌مانند.</summary>
    public UpsertUiPreferenceCommandValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty().WithErrorCode("ui_preference.validation.actor_required");
        RuleFor(x => x.Key).NotEmpty().WithErrorCode("ui_preference.validation.key_required");
        RuleFor(x => x.JsonPayload).NotEmpty().WithErrorCode("ui_preference.validation.json_required");
    }
}
