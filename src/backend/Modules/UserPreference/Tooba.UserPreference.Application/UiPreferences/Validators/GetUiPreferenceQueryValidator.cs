using FluentValidation;
using Tooba.UserPreference.Application.UiPreferences.Queries;
using Tooba.UserPreference.Contracts.Errors;

namespace Tooba.UserPreference.Application.UiPreferences.Validators;

/// <summary>اعتبارسنجی شکل ورودی خواندن UI preference.</summary>
public sealed class GetUiPreferenceQueryValidator : AbstractValidator<GetUiPreferenceQuery>
{
    /// <summary>قواعد حمل‌ونقل؛ Actor از مرز اعتماد است.</summary>
    public GetUiPreferenceQueryValidator()
    {
        RuleFor(x => x.Key).NotEmpty().WithErrorCode(UserPreferenceErrorCodes.UiKeyRequired);
    }
}
