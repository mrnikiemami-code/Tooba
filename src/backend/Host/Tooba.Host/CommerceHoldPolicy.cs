using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tooba.Cart.Application.Ports;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Order.Application;
using Tooba.Order.Application.Checkout.Abuse;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Checkout.Policies;
using Tooba.Order.Application.Checkout.Process;
using Tooba.Order.Application.PurchaseVerification;
using Tooba.Order.Application.ReservationCycle.Contracts;
using Tooba.Order.Application.ReservationCycle.Policies;
using Tooba.Order.Application.ReservationCycle.Services;
using Tooba.Order.Application.Seller.Policies;
using Tooba.Payment.Contracts.Hold;
using Tooba.Payment.Infrastructure.Providers;

namespace Tooba.Host;

/// <summary>
/// حل مهلت پرداخت/رزرو: روش پرداخت &gt; فروشگاه &gt; Payment:Gateway.
/// ماندگاری سبد اینجا محاسبه نمی‌شود؛ مالک آن Cart است.
/// </summary>
public sealed class CommerceHoldPolicy : ICommerceHoldPolicySource, ICheckoutReservationHoldPolicy
{
    private readonly PaymentGatewayOptions _gateway;
    private readonly ICartPersistenceHoursSource _cartPersistence;
    private readonly CatalogDbContext _catalog;
    private readonly IPaymentHoldSettingsGateway _paymentHolds;
    private StoreHoldPolicySettings? _store;
    private Dictionary<string, PaymentMethodHoldOverride>? _methods;
    private bool _loaded;

    /// <summary>سیاست را به Options و درگاه تنظیمات Payment وصل می‌کند.</summary>
    public CommerceHoldPolicy(
        IOptions<PaymentGatewayOptions> gateway,
        ICartPersistenceHoursSource cartPersistence,
        CatalogDbContext catalog,
        IPaymentHoldSettingsGateway paymentHolds)
    {
        _gateway = gateway.Value;
        _cartPersistence = cartPersistence;
        _catalog = catalog;
        _paymentHolds = paymentHolds;
    }

    /// <inheritdoc />
    /// <remarks>Cart owns the persistence policy; Host only forwards the Cart-owned value.</remarks>
    public int ResolveCartPersistenceHours() => _cartPersistence.ResolvePersistenceHours();

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

    private void Load()
    {
        if (_loaded)
        {
            return;
        }

        _store = _catalog.StoreHoldPolicySettings.AsNoTracking()
            .SingleOrDefault(x => x.SettingsId == StoreHoldPolicySettings.SingletonId);
        var methods = _paymentHolds.ListMethodOverridesAsync(CancellationToken.None).GetAwaiter().GetResult();
        _methods = methods.ToDictionary(x => x.ProviderCode, StringComparer.OrdinalIgnoreCase);
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
