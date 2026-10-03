using FluentValidation;
using Tooba.UserPreference.Application.UiPreferences.Commands;
using Tooba.UserPreference.Contracts.Errors;

namespace Tooba.UserPreference.Application.UiPreferences.Validators;

/// <summary>اعتبارسنجی شکل ورودی UI preference.</summary>
public sealed class UpsertUiPreferenceCommandValidator : AbstractValidator<UpsertUiPreferenceCommand>
{
    /// <summary>قواعد حمل‌ونقل؛ قواعد دامنه در Domain می‌مانند.</summary>
    public UpsertUiPreferenceCommandValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty().WithErrorCode(UserPreferenceErrorCodes.UiActorRequired);
        RuleFor(x => x.Key).NotEmpty().WithErrorCode(UserPreferenceErrorCodes.UiKeyRequired);
        RuleFor(x => x.JsonPayload).NotEmpty().WithErrorCode(UserPreferenceErrorCodes.UiJsonRequiredValidation);
    }
}
