using System.Text.Json;
using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.UserPreference.Application.Composition;
using Tooba.UserPreference.Application.Models;
using Tooba.UserPreference.Application.Ports;

namespace Tooba.UserPreference.Application.UiPreferences.Commands;

/// <summary>ایجاد/به‌روزرسانی ترجیح UI کلیددار.</summary>
public sealed record UpsertUiPreferenceCommand(Guid ActorUserId, string Key, string JsonPayload)
    : IRequest<Result<UiPreferenceResponse>>;

/// <summary>Handler نوشتن UI preference.</summary>
public sealed class UpsertUiPreferenceCommandHandler(IUiPreferenceDirectory directory)
    : IRequestHandler<UpsertUiPreferenceCommand, Result<UiPreferenceResponse>>
{
    /// <inheritdoc />
    public Task<Result<UiPreferenceResponse>> Handle(
        UpsertUiPreferenceCommand request,
        CancellationToken cancellationToken) =>
        UserPreferenceOperation.ExecuteAsync(async () =>
        {
            var updated = await directory.UpsertAsync(
                request.ActorUserId,
                request.Key,
                new UiPreferenceWrite(request.JsonPayload),
                cancellationToken);
            using var document = JsonDocument.Parse(updated.JsonPayload);
            return new UiPreferenceResponse(updated.Key, document.RootElement.Clone(), updated.UpdatedAt);
        });
}
