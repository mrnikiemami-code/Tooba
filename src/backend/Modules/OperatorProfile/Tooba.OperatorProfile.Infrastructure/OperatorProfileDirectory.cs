using Microsoft.EntityFrameworkCore;
using Tooba.OperatorProfile.Application;
using Tooba.OperatorProfile.Infrastructure.Persistence;

namespace Tooba.OperatorProfile.Infrastructure;

/// <summary>پیاده‌سازی پروفایل اپراتور که فقط schema خود را لمس می‌کند و مالکیت را سرورمحور اعمال می‌کند.</summary>
public sealed class OperatorProfileDirectory : IOperatorProfileDirectory
{
    private readonly OperatorProfileDbContext _db;

    /// <summary>DbContext مالک را دریافت می‌کند.</summary>
    public OperatorProfileDirectory(OperatorProfileDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<OperatorProfileSnapshot?> GetAsync(Guid actorUserId, CancellationToken cancellationToken)
    {
        EnsureActor(actorUserId);
        var profile = await _db.Profiles.AsNoTracking()
            .SingleOrDefaultAsync(x => x.OwnerUserId == actorUserId, cancellationToken);
        return profile is null ? null : Map(profile);
    }

    /// <inheritdoc />
    public async Task<OperatorProfileSnapshot> UpsertAsync(
        Guid actorUserId,
        OperatorProfileWrite input,
        CancellationToken cancellationToken)
    {
        EnsureActor(actorUserId);
        var now = DateTimeOffset.UtcNow;
        var profile = await _db.Profiles.SingleOrDefaultAsync(x => x.OwnerUserId == actorUserId, cancellationToken);
        if (profile is null)
        {
            profile = Domain.OperatorProfile.Create(
                actorUserId,
                input.DisplayName,
                input.FirstName,
                input.LastName,
                input.Bio,
                now);
            _db.Profiles.Add(profile);
        }
        else
        {
            profile.Update(
                input.DisplayName,
                input.FirstName,
                input.LastName,
                input.Bio,
                now);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Map(profile);
    }

    private static OperatorProfileSnapshot Map(Domain.OperatorProfile profile) =>
        new(
            profile.FirstName,
            profile.LastName,
            profile.DisplayName,
            profile.Bio,
            profile.CreatedAt,
            profile.UpdatedAt);

    private static void EnsureActor(Guid actorUserId)
    {
        if (actorUserId == Guid.Empty)
        {
            throw new InvalidOperationException("Actor معتبر الزامی است.");
        }
    }
}
