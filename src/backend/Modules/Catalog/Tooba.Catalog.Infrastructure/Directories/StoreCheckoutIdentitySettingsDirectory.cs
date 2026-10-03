using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Settings.CheckoutIdentity.Models;
using Tooba.Catalog.Application.Settings.CheckoutIdentity.Ports;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure.Directories;

/// <summary>Catalog persistence for store checkout-identity Admin settings.</summary>
public sealed class StoreCheckoutIdentitySettingsDirectory : IStoreCheckoutIdentitySettingsDirectory
{
    private readonly CatalogDbContext _catalog;
    private readonly IClock _clock;

    /// <summary>Creates the directory.</summary>
    public StoreCheckoutIdentitySettingsDirectory(CatalogDbContext catalog, IClock clock)
    {
        _catalog = catalog;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<CheckoutIdentitySettingsView> GetAsync(CancellationToken cancellationToken)
    {
        var row = await _catalog.StoreCheckoutIdentitySettings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.SettingsId == StoreCheckoutIdentitySettings.SingletonId, cancellationToken);
        return ToView(row?.Policy ?? CheckoutIdentityPolicyKind.AuthenticatedOnly);
    }

    /// <inheritdoc />
    public async Task<Result<CheckoutIdentitySettingsView>> SaveAsync(
        string? policy,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var kind = ParsePolicy(policy);
        var row = await _catalog.StoreCheckoutIdentitySettings
            .SingleOrDefaultAsync(x => x.SettingsId == StoreCheckoutIdentitySettings.SingletonId, cancellationToken);
        if (row is null)
        {
            row = StoreCheckoutIdentitySettings.CreateDefault(now);
            _catalog.StoreCheckoutIdentitySettings.Add(row);
        }

        row.Replace(kind, now);
        await _catalog.SaveChangesAsync(cancellationToken);
        return Result.Success(ToView(row.Policy));
    }

    private static CheckoutIdentityPolicyKind ParsePolicy(string? raw) =>
        string.Equals(raw, nameof(CheckoutIdentityPolicyKind.GuestAllowed), StringComparison.OrdinalIgnoreCase)
            ? CheckoutIdentityPolicyKind.GuestAllowed
            : CheckoutIdentityPolicyKind.AuthenticatedOnly;

    private static CheckoutIdentitySettingsView ToView(CheckoutIdentityPolicyKind policy) =>
        policy == CheckoutIdentityPolicyKind.GuestAllowed
            ? new(
                nameof(CheckoutIdentityPolicyKind.GuestAllowed),
                "خرید مهمان مجاز",
                "Guest checkout allowed",
                "خرید مهمان می‌تواند کنترل سفارش‌های پرداخت‌نشده و محدودیت‌های رزرو موجودی را کاهش دهد.")
            : new(
                nameof(CheckoutIdentityPolicyKind.AuthenticatedOnly),
                "ورود الزامی",
                "Login required",
                null);
}
