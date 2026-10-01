using MediatR;
using Tooba.UserPreference.Application;

namespace Tooba.UserPreference.Application.LocalePreferences.Queries;

/// <summary>خواندن ترجیح locale برای Actor.</summary>
public sealed record GetUserPreferenceQuery(Guid ActorUserId) : IRequest<UserPreferenceSnapshot?>;

/// <summary>Handler خواندن locale.</summary>
public sealed class GetUserPreferenceQueryHandler(IUserPreferenceDirectory directory)
    : IRequestHandler<GetUserPreferenceQuery, UserPreferenceSnapshot?>
{
    /// <inheritdoc />
    public Task<UserPreferenceSnapshot?> Handle(GetUserPreferenceQuery request, CancellationToken cancellationToken)
        => directory.GetAsync(request.ActorUserId, cancellationToken);
}
