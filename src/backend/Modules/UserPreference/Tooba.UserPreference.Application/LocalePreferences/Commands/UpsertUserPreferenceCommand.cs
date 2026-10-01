using FluentValidation;
using MediatR;
using Tooba.UserPreference.Application;

namespace Tooba.UserPreference.Application.LocalePreferences.Commands;

/// <summary>ایجاد/به‌روزرسانی ترجیح locale برای Actor.</summary>
public sealed record UpsertUserPreferenceCommand(Guid ActorUserId, string Locale) : IRequest<UserPreferenceSnapshot>;

/// <summary>Handler نوشتن locale.</summary>
public sealed class UpsertUserPreferenceCommandHandler(IUserPreferenceDirectory directory)
    : IRequestHandler<UpsertUserPreferenceCommand, UserPreferenceSnapshot>
{
    /// <inheritdoc />
    public Task<UserPreferenceSnapshot> Handle(UpsertUserPreferenceCommand request, CancellationToken cancellationToken)
        => directory.UpsertAsync(request.ActorUserId, new UserPreferenceWrite(request.Locale), cancellationToken);
}

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
