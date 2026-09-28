using Microsoft.Extensions.Options;
using Tooba.Catalog.Contracts.Reservation;
using Tooba.Payment.Application.Ports;
using Tooba.Payment.Contracts.Hold;
using Tooba.Payment.Infrastructure.Providers;

namespace Tooba.Payment.Infrastructure.Adapters;

/// <summary>
/// Canonical Payment-owned hold-policy source.
/// Payment owns platform/method defaults; Catalog is consumed only through the read-only Contracts seam.
/// </summary>
internal sealed class CommerceHoldPolicySource : ICommerceHoldPolicySource
{
    private readonly PaymentGatewayOptions _gateway;
    private readonly IStoreHoldPolicyHoursReader _storeHolds;
    private readonly IPaymentHoldSettingsDirectory _paymentHolds;
    private StoreHoldPolicyHoursSnapshot? _store;
    private Dictionary<string, PaymentMethodHoldOverrideDto>? _methods;
    private bool _loaded;

    public CommerceHoldPolicySource(
        IOptions<PaymentGatewayOptions> gateway,
        IStoreHoldPolicyHoursReader storeHolds,
        IPaymentHoldSettingsDirectory paymentHolds)
    {
        _gateway = gateway.Value;
        _storeHolds = storeHolds;
        _paymentHolds = paymentHolds;
    }

    public int ResolveOnlineHoldHours(string? providerCode)
    {
        Load();
        var method = FindMethod(providerCode);
        var platform = Clamp(_gateway.OnlinePaymentHoldHours, 1, 24 * 30);
        var store = _store?.OnlinePaymentHoldHours ?? _gateway.OrderSupplyHoldOverrides?.OnlinePaymentHoldHours;
        return Clamp(method?.OnlinePaymentHoldHours ?? store ?? platform, 1, 24 * 30);
    }

    public int ResolveManualInitialHoldHours(string? providerCode)
    {
        Load();
        var method = FindMethod(providerCode);
        var platform = Clamp(_gateway.ManualPaymentInitialHoldHours, 1, 24 * 30);
        var store = _store?.ManualPaymentInitialHoldHours ?? _gateway.OrderSupplyHoldOverrides?.ManualPaymentInitialHoldHours;
        return Clamp(method?.ManualPaymentInitialHoldHours ?? store ?? platform, 1, 24 * 30);
    }

    public int ResolveManualReviewHoldHours(string? providerCode)
    {
        Load();
        var method = FindMethod(providerCode);
        var platform = Clamp(_gateway.ManualPaymentReviewHoldHours, 1, 24 * 30);
        var store = _store?.ManualPaymentReviewHoldHours ?? _gateway.OrderSupplyHoldOverrides?.ManualPaymentReviewHoldHours;
        return Clamp(method?.ManualPaymentReviewHoldHours ?? store ?? platform, 1, 24 * 30);
    }

    public DateTimeOffset ResolveInitialExpiresAt(DateTimeOffset utcNow)
    {
        var online = ResolveOnlineHoldHours(null);
        var manual = ResolveManualInitialHoldHours("manual");
        return utcNow.AddHours(Math.Max(online, manual));
    }

    public DateTimeOffset ResolveUnpaidTimeoutAt(string providerCode, DateTimeOffset utcNow)
    {
        if (string.Equals(providerCode, "manual", StringComparison.OrdinalIgnoreCase))
        {
            return utcNow.AddHours(ResolveManualInitialHoldHours(providerCode));
        }

        return utcNow.AddHours(ResolveOnlineHoldHours(providerCode));
    }

    public DateTimeOffset ResolveManualReviewExpiresAt(DateTimeOffset utcNow) =>
        utcNow.AddHours(ResolveManualReviewHoldHours("manual"));

    private void Load()
    {
        if (_loaded)
        {
            return;
        }

        _store = _storeHolds.GetHoursAsync(CancellationToken.None).GetAwaiter().GetResult();
        var methods = _paymentHolds.ListMethodOverridesAsync(CancellationToken.None).GetAwaiter().GetResult();
        _methods = methods.ToDictionary(x => x.ProviderCode, StringComparer.OrdinalIgnoreCase);
        _loaded = true;
    }

    private PaymentMethodHoldOverrideDto? FindMethod(string? providerCode)
    {
        if (string.IsNullOrWhiteSpace(providerCode) || _methods is null)
        {
            return null;
        }

        return _methods.TryGetValue(providerCode.Trim(), out var row) ? row : null;
    }

    private static int Clamp(int value, int min, int max) =>
        Math.Clamp(value <= 0 ? min : value, min, max);
}
