using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Identity.Application;
using Tooba.Identity.Domain;
using Tooba.Identity.Infrastructure.Persistence;

namespace Tooba.Identity.Infrastructure.ExternalIdentity;

/// <summary>
/// ذخیرهٔ اتصال IdP روی جدول Identity.
/// </summary>
public sealed class EfExternalIdentityDirectory : IExternalIdentityDirectory
{
    private readonly IdentityDbContext _db;

    /// <summary>
    /// فهرست را روی DbContext ماژول می‌سازد.
    /// </summary>
    public EfExternalIdentityDirectory(IdentityDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task BindAsync(Guid userId, string issuer, string subject, CancellationToken cancellationToken)
    {
        _db.ExternalBindings.Add(new ExternalIdentityBinding
        {
            Id = UuidV7.New(),
            UserId = userId,
            Issuer = issuer.Trim(),
            Subject = subject.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Guid?> FindUserIdAsync(string issuer, string subject, CancellationToken cancellationToken)
    {
        var row = await _db.ExternalBindings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Issuer == issuer.Trim() && x.Subject == subject.Trim(), cancellationToken);
        return row?.UserId;
    }
}
