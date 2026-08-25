using Microsoft.EntityFrameworkCore;
using Tooba.CustomerProfile.Application;
using Tooba.CustomerProfile.Domain;
using Tooba.CustomerProfile.Infrastructure.Persistence;

namespace Tooba.CustomerProfile.Infrastructure;

/// <summary>پیاده‌سازی پروفایل که فقط schema خود را لمس می‌کند و مالکیت را سرورمحور اعمال می‌کند.</summary>
public sealed class CustomerProfileDirectory : ICustomerProfileDirectory
{
    private readonly CustomerProfileDbContext _db;

    /// <summary>DbContext مالک را دریافت می‌کند.</summary>
    public CustomerProfileDirectory(CustomerProfileDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<CustomerProfileSnapshot?> GetAsync(Guid actorUserId, CancellationToken cancellationToken)
    {
        EnsureActor(actorUserId);
        var profile = await _db.Profiles.AsNoTracking()
            .SingleOrDefaultAsync(x => x.OwnerUserId == actorUserId, cancellationToken);
        return profile is null ? null : Map(profile);
    }

    /// <inheritdoc />
    public async Task<CustomerProfileSnapshot> UpsertAsync(
        Guid actorUserId,
        CustomerProfileWrite input,
        CancellationToken cancellationToken)
    {
        EnsureActor(actorUserId);
        var now = DateTimeOffset.UtcNow;
        var profile = await _db.Profiles.SingleOrDefaultAsync(x => x.OwnerUserId == actorUserId, cancellationToken);
        if (profile is null)
        {
            profile = Tooba.CustomerProfile.Domain.CustomerProfile.Create(
                actorUserId,
                input.DisplayName,
                input.FirstName,
                input.LastName,
                input.BirthDate,
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
                input.BirthDate,
                input.Bio,
                now);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Map(profile);
    }

    private static CustomerProfileSnapshot Map(Tooba.CustomerProfile.Domain.CustomerProfile profile) =>
        new(
            profile.FirstName,
            profile.LastName,
            profile.DisplayName,
            profile.BirthDate,
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
