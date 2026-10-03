using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.UserPreference.Application.Composition;
using Tooba.UserPreference.Application.Models;
using Tooba.UserPreference.Application.Ports;

namespace Tooba.UserPreference.Application.LocalePreferences.Queries;

/// <summary>خواندن ترجیح locale برای Actor.</summary>
public sealed record GetUserPreferenceQuery(Guid ActorUserId) : IRequest<Result<PreferenceLocaleResponse>>;

/// <summary>Handler خواندن locale.</summary>
public sealed class GetUserPreferenceQueryHandler(IUserPreferenceDirectory directory)
    : IRequestHandler<GetUserPreferenceQuery, Result<PreferenceLocaleResponse>>
{
    /// <inheritdoc />
    public Task<Result<PreferenceLocaleResponse>> Handle(
        GetUserPreferenceQuery request,
        CancellationToken cancellationToken) =>
        UserPreferenceOperation.ExecuteAsync(async () =>
        {
            var snapshot = await directory.GetAsync(request.ActorUserId, cancellationToken);
            return snapshot is null
                ? new PreferenceLocaleResponse(UserPreferenceShapes.DefaultLocale, null, null)
                : new PreferenceLocaleResponse(snapshot.Locale, snapshot.CreatedAt, snapshot.UpdatedAt);
        });
}
