namespace Tooba.Fulfillment.Application.Ports;

/// <summary>
/// نگهبان use-case fulfillment.
/// </summary>
public interface IFulfillmentUseCaseGuard
{
    /// <summary>اجازهٔ mutate را بررسی می‌کند.</summary>
    Task EnsureCanMutateAsync(CancellationToken cancellationToken);
}
