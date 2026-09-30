#pragma warning disable CS1591
using Tooba.Payment.Contracts.Ports;

namespace Tooba.Host.Security.Checkout;

/// <summary>Thin Host adapter: Payment checkout actor policy → CheckoutIdentityGate.</summary>
public sealed class HostCheckoutActorPolicyAdapter(CheckoutIdentityGate gate) : ICheckoutActorPolicyPort
{
    public Task EnsureCheckoutActorAsync(CancellationToken cancellationToken) =>
        gate.EnsureCheckoutActorAsync(cancellationToken);
}
