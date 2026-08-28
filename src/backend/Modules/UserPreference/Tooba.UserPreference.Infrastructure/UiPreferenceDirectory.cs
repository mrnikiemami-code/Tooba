using Microsoft.EntityFrameworkCore;
using Tooba.UserPreference.Application;
using Tooba.UserPreference.Domain;
using Tooba.UserPreference.Infrastructure.Persistence;

namespace Tooba.UserPreference.Infrastructure;

/// <summary>پیاده‌سازی ترجیح کلیددار UI با مالکیت سرورمحور.</summary>
public sealed class UiPreferenceDirectory : IUiPreferenceDirectory
{
    private readonly UserPreferenceDbContext _db;

    /// <summary>DbContext مالک را دریافت می‌کند.</summary>
    public UiPreferenceDirectory(UserPreferenceDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<UiPreferenceSnapshot?> GetAsync(
        Guid actorUserId,
        string key,
        CancellationToken cancellationToken)
    {
        EnsureActor(actorUserId);
        var normalized = UiPreference.NormalizeKey(key);
        var preference = await _db.UiPreferences.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.ActorUserId == actorUserId && x.Key == normalized,
                cancellationToken);
        return preference is null ? null : Map(preference);
    }

    /// <inheritdoc />
    public async Task<UiPreferenceSnapshot> UpsertAsync(
        Guid actorUserId,
        string key,
        UiPreferenceWrite input,
        CancellationToken cancellationToken)
    {
        EnsureActor(actorUserId);
        var normalized = UiPreference.NormalizeKey(key);
        var now = DateTimeOffset.UtcNow;
        var preference = await _db.UiPreferences
            .SingleOrDefaultAsync(
                x => x.ActorUserId == actorUserId && x.Key == normalized,
                cancellationToken);
        if (preference is null)
        {
            preference = UiPreference.Create(actorUserId, normalized, input.JsonPayload, now);
            _db.UiPreferences.Add(preference);
        }
        else
        {
            preference.Update(input.JsonPayload, now);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Map(preference);
    }

    private static UiPreferenceSnapshot Map(UiPreference preference) =>
        new(preference.Key, preference.JsonPayload, preference.UpdatedAt);

    private static void EnsureActor(Guid actorUserId)
    {
        if (actorUserId == Guid.Empty)
        {
            throw new InvalidOperationException("Actor معتبر الزامی است.");
        }
    }
}
