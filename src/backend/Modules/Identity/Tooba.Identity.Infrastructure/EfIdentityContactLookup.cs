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

        var identifiers = await _db.Identifiers.AsNoTracking()
            .Where(x => x.UserId == userId && (x.Kind == LoginIdentifierKind.Email || x.Kind == LoginIdentifierKind.Phone))
            .ToListAsync(cancellationToken);
        var email = identifiers.FirstOrDefault(x => x.Kind == LoginIdentifierKind.Email)?.DisplayValue;
        var mobile = identifiers.FirstOrDefault(x => x.Kind == LoginIdentifierKind.Phone)?.DisplayValue;
        return new IdentityContactSnapshot(email, mobile);
    }
}
