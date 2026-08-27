using Microsoft.EntityFrameworkCore;
using Tooba.UserPreference.Application;
using Tooba.UserPreference.Infrastructure.Persistence;

namespace Tooba.UserPreference.Infrastructure;

/// <summary>پیاده‌سازی ترجیح که فقط schema خود را لمس می‌کند و مالکیت را سرورمحور اعمال می‌کند.</summary>
public sealed class UserPreferenceDirectory : IUserPreferenceDirectory
{
    private readonly UserPreferenceDbContext _db;

    /// <summary>DbContext مالک را دریافت می‌کند.</summary>
    public UserPreferenceDirectory(UserPreferenceDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<UserPreferenceSnapshot?> GetAsync(Guid actorUserId, CancellationToken cancellationToken)
    {
        EnsureActor(actorUserId);
        var preference = await _db.Preferences.AsNoTracking()
            .SingleOrDefaultAsync(x => x.OwnerUserId == actorUserId, cancellationToken);
        return preference is null ? null : Map(preference);
    }

    /// <inheritdoc />
    public async Task<UserPreferenceSnapshot> UpsertAsync(
        Guid actorUserId,
        UserPreferenceWrite input,
        CancellationToken cancellationToken)
    {
        EnsureActor(actorUserId);
        var now = DateTimeOffset.UtcNow;
        var preference = await _db.Preferences.SingleOrDefaultAsync(x => x.OwnerUserId == actorUserId, cancellationToken);
        if (preference is null)
        {
            preference = Domain.UserPreference.Create(actorUserId, input.Locale, now);
            _db.Preferences.Add(preference);
        }
        else
        {
            preference.Update(input.Locale, now);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Map(preference);
    }

    private static UserPreferenceSnapshot Map(Domain.UserPreference preference) =>
        new(preference.Locale, preference.CreatedAt, preference.UpdatedAt);

    private static void EnsureActor(Guid actorUserId)
    {
        if (actorUserId == Guid.Empty)
        {
            throw new InvalidOperationException("Actor معتبر الزامی است.");
        }
    }
}
