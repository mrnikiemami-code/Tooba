using System.Text.Json;
using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.UserPreference.Application.Composition;
using Tooba.UserPreference.Application.Models;
using Tooba.UserPreference.Application.Ports;
using Tooba.UserPreference.Contracts.Errors;

namespace Tooba.UserPreference.Application.UiPreferences.Queries;

/// <summary>خواندن ترجیح UI کلیددار برای Actor.</summary>
public sealed record GetUiPreferenceQuery(Guid ActorUserId, string Key) : IRequest<Result<UiPreferenceResponse>>;

/// <summary>Handler خواندن UI preference.</summary>
public sealed class GetUiPreferenceQueryHandler(IUiPreferenceDirectory directory)
    : IRequestHandler<GetUiPreferenceQuery, Result<UiPreferenceResponse>>
{
    /// <inheritdoc />
    public Task<Result<UiPreferenceResponse>> Handle(
        GetUiPreferenceQuery request,
        CancellationToken cancellationToken) =>
        UserPreferenceOperation.ExecuteAsync(async () =>
        {
            var snapshot = await directory.GetAsync(request.ActorUserId, request.Key, cancellationToken);
            if (snapshot is null)
            {
                var normalized = UserPreferenceShapes.NormalizeUiKey(request.Key);
                return new UiPreferenceResponse(normalized, null, null);
            }

            try
            {
                using var document = JsonDocument.Parse(snapshot.JsonPayload);
                return new UiPreferenceResponse(snapshot.Key, document.RootElement.Clone(), snapshot.UpdatedAt);
            }
            catch (JsonException)
            {
                throw new SemanticException(new SemanticError(UserPreferenceErrorCodes.UiPreferenceInvalidJson));
            }
        });
}
