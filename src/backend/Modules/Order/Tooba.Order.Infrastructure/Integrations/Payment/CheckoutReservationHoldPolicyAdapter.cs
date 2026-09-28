using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Payment.Contracts.Hold;

namespace Tooba.Order.Infrastructure.Integrations.Payment;

/// <summary>
/// Order-side adapter from the Payment Contracts hold-policy source to the Order checkout port.
/// Keeps Order.Application free of Payment implementation details.
/// </summary>
internal sealed class CheckoutReservationHoldPolicyAdapter(
    ICommerceHoldPolicySource source) : ICheckoutReservationHoldPolicy
{
    public DateTimeOffset ResolveInitialExpiresAt(DateTimeOffset utcNow) =>
        source.ResolveInitialExpiresAt(utcNow);
}
