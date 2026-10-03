using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Ports;

/// <summary>
/// درز نگهبان مجوز موردکاربرد. ماتریس نهایی Catalog و SDK اسپایس‌دی‌بی اینجا نیست.
/// </summary>
public interface ICatalogUseCaseGuard
{
    /// <summary>
    /// اجازهٔ نوشتن foundation را بررسی می‌کند. پیاده‌سازی فعلی فقط درز است.
    /// </summary>
    Task EnsureCanMutateAsync(CancellationToken cancellationToken);
}
