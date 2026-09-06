using Microsoft.EntityFrameworkCore;
using Tooba.Identity.Application;
using Tooba.Identity.Domain;
using Tooba.Identity.Infrastructure.Persistence;

namespace Tooba.Identity.Infrastructure;

/// <summary>lookup فقط‌خواندنی شناسه‌های تماس Identity بدون افشای credential.</summary>
public sealed class EfIdentityContactLookup : IIdentityContactLookup
{
    private readonly IdentityDbContext _db;

    /// <summary>DbContext Identity را دریافت می‌کند.</summary>
    public EfIdentityContactLookup(IdentityDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<IdentityContactSnapshot> GetContactAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            return new IdentityContactSnapshot(null, null);
        }

        var map = await GetContactsAsync(new[] { userId }, cancellationToken);
        return map.TryGetValue(userId, out var snapshot)
            ? snapshot
            : new IdentityContactSnapshot(null, null);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, IdentityContactSnapshot>> GetContactsAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken)
    {
        var ids = userIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();
        if (ids.Length == 0)
        {
            return new Dictionary<Guid, IdentityContactSnapshot>();
        }

        var identifiers = await _db.Identifiers.AsNoTracking()
            .Where(x => ids.Contains(x.UserId)
                && (x.Kind == LoginIdentifierKind.Email || x.Kind == LoginIdentifierKind.Phone))
            .ToListAsync(cancellationToken);

        var result = new Dictionary<Guid, IdentityContactSnapshot>(ids.Length);
        foreach (var id in ids)
        {
            var forUser = identifiers.Where(x => x.UserId == id).ToList();
            if (forUser.Count == 0)
            {
                continue;
            }

            var email = forUser.FirstOrDefault(x => x.Kind == LoginIdentifierKind.Email)?.DisplayValue;
            var mobile = forUser.FirstOrDefault(x => x.Kind == LoginIdentifierKind.Phone)?.DisplayValue;
            result[id] = new IdentityContactSnapshot(email, mobile);
        }

        return result;
    }
}
