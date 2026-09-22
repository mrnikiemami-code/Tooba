#pragma warning disable CS1591
using Tooba.Host.Admin;
using Tooba.Inventory.Application.Orders;
using Tooba.Payment.Application.Errors;
using Tooba.Payment.Application.Ports;

namespace Tooba.Host.Storefront;

/// <summary>Host adapter for unpaid-retry supply ensure (Contracts/Host composition only).</summary>
public sealed class HostPaymentUnpaidRetrySupplyAdapter : IPaymentUnpaidRetrySupplyPort
{
    private readonly OrderSupplyComposer? _supply;
    private readonly ReservationCycleCoordinator? _cycles;

    public HostPaymentUnpaidRetrySupplyAdapter(
        OrderSupplyComposer? supply = null,
        ReservationCycleCoordinator? cycles = null)
    {
        _supply = supply;
        _cycles = cycles;
    }

    public async Task EnsureRetrySupplyAsync(Guid checkoutId, CancellationToken cancellationToken)
    {
        if (_supply is null)
            throw new InvalidOperationException(PaymentErrorCodes.UnpaidSupplyUnavailable);

        try
        {
            var result = _cycles is not null
                ? await _cycles.EnsureRetryAfterExpiryAsync(checkoutId, cancellationToken)
                : await _supply.EnsureAsync(
                    checkoutId,
                    OrderSupplyMode.EnsureUnpaidRetryHold,
                    allowReacquire: true,
                    reason: "unpaid-retry",
                    cancellationToken);

            if (result.Status is OrderSupplyStatusKind.Unavailable or OrderSupplyStatusKind.PartiallyUnavailable
                || result.Outcome is OrderSupplyOutcome.Unavailable or OrderSupplyOutcome.PartiallyUnavailable)
            {
                throw new InvalidOperationException(PaymentErrorCodes.UnpaidSupplyUnavailable);
            }
        }
        catch (InvalidOperationException ex) when (
            ex.Message is PaymentErrorCodes.ReservationRetryLimit
                or "inventory.reservation.retry_limit_reached"
                or PaymentErrorCodes.UnpaidSupplyUnavailable)
        {
            throw new InvalidOperationException(
                ex.Message is "inventory.reservation.retry_limit_reached"
                    ? PaymentErrorCodes.ReservationRetryLimit
                    : ex.Message);
        }
    }
}
