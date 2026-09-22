#pragma warning disable CS1591
using Tooba.Payment.Application.Ports;

namespace Tooba.Host.Storefront;

public sealed class HostCheckoutActorPolicyAdapter(CheckoutIdentityGate gate) : ICheckoutActorPolicyPort
{
    public Task EnsureCheckoutActorAsync(CancellationToken cancellationToken) =>
        gate.EnsureCheckoutActorAsync(cancellationToken);
}
