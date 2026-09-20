using Tooba.Order.Application;

namespace Tooba.Order.Infrastructure;

/// <summary>
/// نگهبان باز موردکاربرد Order. ماتریس هویت اینجا نیست.
/// </summary>
public sealed class OpenOrderUseCaseGuard : IOrderUseCaseGuard
{
    /// <inheritdoc />
    public Task EnsureCanMutateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
