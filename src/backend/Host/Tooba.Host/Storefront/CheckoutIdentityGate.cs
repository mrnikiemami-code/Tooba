using Microsoft.EntityFrameworkCore;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Host.Storefront;

/// <summary>
/// سیاست هویت خرید را از تنظیم فروشگاه می‌خواند و در AuthenticatedOnly نشست را الزام می‌کند.
/// </summary>
public sealed class CheckoutIdentityGate
{
    private readonly CatalogDbContext _catalog;
    private readonly CurrentAuthenticatedSession _session;

    /// <summary>دروازه را به تنظیم Catalog و نشست جاری وصل می‌کند.</summary>
    internal CheckoutIdentityGate(CatalogDbContext catalog, CurrentAuthenticatedSession session)
    {
        _catalog = catalog;
        _session = session;
    }

    /// <summary>سیاست مؤثر؛ بدون ردیف یعنی AuthenticatedOnly.</summary>
    public async Task<CheckoutIdentityPolicyKind> GetEffectiveAsync(CancellationToken cancellationToken)
    {
        var row = await _catalog.StoreCheckoutIdentitySettings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.SettingsId == StoreCheckoutIdentitySettings.SingletonId, cancellationToken);
        return row?.Policy ?? CheckoutIdentityPolicyKind.AuthenticatedOnly;
    }

    /// <summary>در AuthenticatedOnly بدون نشست مشتری رد می‌شود.</summary>
    public async Task EnsureCheckoutActorAsync(CancellationToken cancellationToken)
    {
        if (await GetEffectiveAsync(cancellationToken) == CheckoutIdentityPolicyKind.GuestAllowed)
        {
            return;
        }

        if (_session.IsAuthenticated && _session.UserId is Guid userId && userId != Guid.Empty)
        {
            return;
        }

        throw new InvalidOperationException("checkout.authentication_required");
    }
}
