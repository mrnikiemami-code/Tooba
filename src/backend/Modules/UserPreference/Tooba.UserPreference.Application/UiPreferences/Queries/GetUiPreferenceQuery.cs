using MediatR;
using Tooba.UserPreference.Application;

namespace Tooba.UserPreference.Application.UiPreferences.Queries;

/// <summary>خواندن ترجیح UI کلیددار برای Actor.</summary>
public sealed record GetUiPreferenceQuery(Guid ActorUserId, string Key) : IRequest<UiPreferenceSnapshot?>;

/// <summary>Handler خواندن UI preference.</summary>
public sealed class GetUiPreferenceQueryHandler(IUiPreferenceDirectory directory)
    : IRequestHandler<GetUiPreferenceQuery, UiPreferenceSnapshot?>
{
    /// <inheritdoc />
    public Task<UiPreferenceSnapshot?> Handle(GetUiPreferenceQuery request, CancellationToken cancellationToken)
        => directory.GetAsync(request.ActorUserId, request.Key, cancellationToken);
}
