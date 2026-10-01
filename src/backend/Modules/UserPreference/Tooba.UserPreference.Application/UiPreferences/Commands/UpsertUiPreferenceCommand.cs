using FluentValidation;
using MediatR;
using Tooba.UserPreference.Application;

namespace Tooba.UserPreference.Application.UiPreferences.Commands;

/// <summary>ایجاد/به‌روزرسانی ترجیح UI کلیددار.</summary>
public sealed record UpsertUiPreferenceCommand(Guid ActorUserId, string Key, string JsonPayload)
    : IRequest<UiPreferenceSnapshot>;

/// <summary>Handler نوشتن UI preference.</summary>
public sealed class UpsertUiPreferenceCommandHandler(IUiPreferenceDirectory directory)
    : IRequestHandler<UpsertUiPreferenceCommand, UiPreferenceSnapshot>
{
    /// <inheritdoc />
    public Task<UiPreferenceSnapshot> Handle(UpsertUiPreferenceCommand request, CancellationToken cancellationToken)
        => directory.UpsertAsync(
            request.ActorUserId,
            request.Key,
            new UiPreferenceWrite(request.JsonPayload),
            cancellationToken);
}

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
