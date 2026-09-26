using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Identity.Application;
using Tooba.Identity.Domain;
using Tooba.Identity.Infrastructure.Persistence;

namespace Tooba.Identity.Infrastructure.Mfa;

/// <summary>
/// ذخیرهٔ MFA روی جدول Identity.
/// </summary>
public sealed class EfMfaEnrollmentStore : IMfaEnrollmentStore
{
    private readonly IdentityDbContext _db;

    /// <summary>
    /// فروشگاه را روی DbContext ماژول می‌سازد.
    /// </summary>
    public EfMfaEnrollmentStore(IdentityDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task EnrollAsync(Guid userId, MfaFactorKind factorKind, CancellationToken cancellationToken)
    {
        _db.MfaEnrollments.Add(new MfaFactorEnrollment
        {
            Id = UuidV7.New(),
            UserId = userId,
            FactorKind = factorKind,
            IsEnabled = true,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MfaFactorKind>> ListEnabledAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _db.MfaEnrollments.AsNoTracking()
            .Where(x => x.UserId == userId && x.IsEnabled)
            .Select(x => x.FactorKind)
            .ToListAsync(cancellationToken);
    }
}
