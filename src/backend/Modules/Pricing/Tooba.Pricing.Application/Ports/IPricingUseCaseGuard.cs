namespace Tooba.Pricing.Application.Ports;

/// <summary>
/// Pricing use-case authorization seam. The price admin matrix is not implemented here; the seam
/// exists so the Infrastructure directory never owns an authorization decision.
/// </summary>
public interface IPricingUseCaseGuard
{
    /// <summary>
    /// Verifies that the current actor may write an authored price. The current implementation is an
    /// open seam only.
    /// </summary>
    Task EnsureCanMutateAsync(CancellationToken cancellationToken);
}
