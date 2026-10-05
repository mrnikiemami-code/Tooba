using Tooba.Inventory.Application.Ports;

namespace Tooba.Inventory.Infrastructure.Directories;

/// <summary>
/// نگهبان باز موردکاربرد. ماتریس انبار اینجا نیست.
/// در فایل مستقل قرار دارد تا <see cref="InventoryDirectory"/> یک دلیل تغییر داشته باشد.
/// </summary>
public sealed class OpenInventoryUseCaseGuard : IInventoryUseCaseGuard
{
    /// <inheritdoc />
    public Task EnsureCanMutateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
