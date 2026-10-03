using MediatR;
using Tooba.UserPreference.Application.Models;
using Tooba.UserPreference.Application.Ports;

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
