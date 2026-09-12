using Tooba.Order.Application;
using Tooba.Payment.Infrastructure;

namespace Tooba.Host;

/// <summary>
/// مهلت رزرو اولیهٔ سفارش از Payment:Gateway + override فروشگاه.
/// </summary>
public sealed class CheckoutReservationHoldPolicy : ICheckoutReservationHoldPolicy
{
    private readonly PaymentGatewayOptions _gateway;

    /// <summary>سیاست را به تنظیمات درگاه وصل می‌کند.</summary>
    public CheckoutReservationHoldPolicy(Microsoft.Extensions.Options.IOptions<PaymentGatewayOptions> gateway)
    {
        _gateway = gateway.Value;
    }

    /// <inheritdoc />
    public DateTimeOffset ResolveInitialExpiresAt(DateTimeOffset utcNow)
    {
        var online = Math.Clamp(_gateway.OnlinePaymentHoldHours, 1, 24 * 30);
        var manual = Math.Clamp(_gateway.ManualPaymentInitialHoldHours, 1, 24 * 30);
        if (_gateway.OrderSupplyHoldOverrides is { } o)
        {
            if (o.OnlinePaymentHoldHours is int ovOnline)
            {
                online = Math.Clamp(ovOnline, 1, 24 * 30);
            }

            if (o.ManualPaymentInitialHoldHours is int ovManual)
            {
                manual = Math.Clamp(ovManual, 1, 24 * 30);
            }
        }

        return utcNow.AddHours(Math.Max(online, manual));
    }
}
