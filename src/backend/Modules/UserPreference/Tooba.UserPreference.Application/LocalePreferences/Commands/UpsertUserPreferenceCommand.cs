using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.UserPreference.Application.Composition;
using Tooba.UserPreference.Application.Models;
using Tooba.UserPreference.Application.Ports;

namespace Tooba.UserPreference.Application.LocalePreferences.Commands;

/// <summary>ایجاد/به‌روزرسانی ترجیح locale برای Actor.</summary>
public sealed record UpsertUserPreferenceCommand(Guid ActorUserId, string Locale)
    : IRequest<Result<PreferenceLocaleResponse>>;

/// <summary>Handler نوشتن locale.</summary>
public sealed class UpsertUserPreferenceCommandHandler(IUserPreferenceDirectory directory)
    : IRequestHandler<UpsertUserPreferenceCommand, Result<PreferenceLocaleResponse>>
{
    /// <inheritdoc />
    public Task<Result<PreferenceLocaleResponse>> Handle(
        UpsertUserPreferenceCommand request,
        CancellationToken cancellationToken) =>
        UserPreferenceOperation.ExecuteAsync(async () =>
        {
            var updated = await directory.UpsertAsync(
                request.ActorUserId,
                new UserPreferenceWrite(request.Locale),
                cancellationToken);
            return new PreferenceLocaleResponse(updated.Locale, updated.CreatedAt, updated.UpdatedAt);
        });
}
