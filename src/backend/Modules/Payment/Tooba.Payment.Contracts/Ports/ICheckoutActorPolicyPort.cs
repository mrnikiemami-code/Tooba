namespace Tooba.Payment.Contracts.Ports;

/// <summary>Host-enforced checkout actor policy seam owned by Payment Contracts.</summary>
public interface ICheckoutActorPolicyPort
{
    Task EnsureCheckoutActorAsync(CancellationToken cancellationToken);
}
