namespace Tooba.Inventory.Application.Ports;

/// <summary>
/// درز نگهبان مجوز Inventory. ماتریس انبار اینجا نیست.
/// </summary>
public interface IInventoryUseCaseGuard
{
    /// <summary>
    /// اجازهٔ نوشتن موجودی را بررسی می‌کند. پیاده‌سازی فعلی فقط درز است.
    /// </summary>
    Task EnsureCanMutateAsync(CancellationToken cancellationToken);
}
