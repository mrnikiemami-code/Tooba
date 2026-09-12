using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tooba.Cart.Application;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Order.Application;
using Tooba.Payment.Application;
using Tooba.Payment.Domain;
using Tooba.Payment.Infrastructure;
using Tooba.Payment.Infrastructure.Persistence;

namespace Tooba.Host;

/// <summary>
/// حل مهلت: روش پرداخت &gt; فروشگاه &gt; Payment:Gateway / Cart:PersistenceHours.
/// </summary>
public sealed class CommerceHoldPolicy : ICommerceHoldPolicy, ICheckoutReservationHoldPolicy, ICartPersistenceHoursSource
{
    private readonly PaymentGatewayOptions _gateway;
    private readonly CartLifetimeOptions _cart;
    private readonly CatalogDbContext _catalog;
    private readonly PaymentDbContext _payments;
    private StoreHoldPolicySettings? _store;
    private Dictionary<string, PaymentMethodHoldOverride>? _methods;
    private bool _loaded;

    /// <summary>سیاست را به Options و ردیف‌های Settings وصل می‌کند.</summary>
    public CommerceHoldPolicy(
        IOptions<PaymentGatewayOptions> gateway,
        IOptions<CartLifetimeOptions> cart,
        CatalogDbContext catalog,
        PaymentDbContext payments)
    {
        _gateway = gateway.Value;
        _cart = cart.Value;
        _catalog = catalog;
        _payments = payments;
    }

    /// <inheritdoc />
    public int ResolveCartPersistenceHours()
    {
        Load();
        var platform = Math.Clamp(_cart.PersistenceHours <= 0 ? 168 : _cart.PersistenceHours, 1, 24 * 90);
        return Clamp(_store?.CartPersistenceHours ?? platform, 1, 24 * 90);
    }

    /// <inheritdoc />
    public int ResolveOnlineHoldHours(string? providerCode)
    {
        Load();
        var method = FindMethod(providerCode);
        var platform = Clamp(_gateway.OnlinePaymentHoldHours, 1, 24 * 30);
        var store = _store?.OnlinePaymentHoldHours ?? _gateway.OrderSupplyHoldOverrides?.OnlinePaymentHoldHours;
        return Clamp(method?.OnlinePaymentHoldHours ?? store ?? platform, 1, 24 * 30);
    }

    /// <inheritdoc />
    public int ResolveManualInitialHoldHours(string? providerCode)
    {
        Load();
        var method = FindMethod(providerCode);
        var platform = Clamp(_gateway.ManualPaymentInitialHoldHours, 1, 24 * 30);
        var store = _store?.ManualPaymentInitialHoldHours ?? _gateway.OrderSupplyHoldOverrides?.ManualPaymentInitialHoldHours;
        return Clamp(method?.ManualPaymentInitialHoldHours ?? store ?? platform, 1, 24 * 30);
    }

    /// <inheritdoc />
    public int ResolveManualReviewHoldHours(string? providerCode)
    {
        Load();
        var method = FindMethod(providerCode);
        var platform = Clamp(_gateway.ManualPaymentReviewHoldHours, 1, 24 * 30);
        var store = _store?.ManualPaymentReviewHoldHours ?? _gateway.OrderSupplyHoldOverrides?.ManualPaymentReviewHoldHours;
        return Clamp(method?.ManualPaymentReviewHoldHours ?? store ?? platform, 1, 24 * 30);
    }

    /// <inheritdoc />
    public DateTimeOffset ResolveInitialExpiresAt(DateTimeOffset utcNow)
    {
        var online = ResolveOnlineHoldHours(null);
        var manual = ResolveManualInitialHoldHours("manual");
        return utcNow.AddHours(Math.Max(online, manual));
    }

    /// <inheritdoc />
    public DateTimeOffset ResolveUnpaidTimeoutAt(string providerCode, DateTimeOffset utcNow)
    {
        if (string.Equals(providerCode, "manual", StringComparison.OrdinalIgnoreCase))
        {
            return utcNow.AddHours(ResolveManualInitialHoldHours(providerCode));
        }

        return utcNow.AddHours(ResolveOnlineHoldHours(providerCode));
    }

    /// <inheritdoc />
    public DateTimeOffset ResolveManualReviewExpiresAt(DateTimeOffset utcNow) =>
        utcNow.AddHours(ResolveManualReviewHoldHours("manual"));

    /// <inheritdoc />
    public int ResolvePersistenceHours() => ResolveCartPersistenceHours();

    private void Load()
    {
        if (_loaded)
        {
            return;
        }

        _store = _catalog.StoreHoldPolicySettings.AsNoTracking()
            .SingleOrDefault(x => x.SettingsId == StoreHoldPolicySettings.SingletonId);
        _methods = _payments.MethodHoldOverrides.AsNoTracking()
            .ToDictionary(x => x.ProviderCode, StringComparer.OrdinalIgnoreCase);
        _loaded = true;
    }

    private PaymentMethodHoldOverride? FindMethod(string? providerCode)
    {
        if (string.IsNullOrWhiteSpace(providerCode) || _methods is null)
        {
            return null;
        }

        return _methods.TryGetValue(providerCode.Trim(), out var row) ? row : null;
    }

    private static int Clamp(int value, int min, int max) => Math.Clamp(value <= 0 ? min : value, min, max);
}
