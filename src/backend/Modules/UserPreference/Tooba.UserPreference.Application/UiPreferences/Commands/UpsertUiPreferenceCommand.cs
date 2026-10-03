using MediatR;
using Tooba.UserPreference.Application.Models;
using Tooba.UserPreference.Application.Ports;

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
