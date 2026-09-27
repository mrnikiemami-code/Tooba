using Tooba.BuildingBlocks;
using Tooba.Catalog.Application.Settings.StoreAppearance.Models;

namespace Tooba.Catalog.Application.Settings.StoreAppearance.Ports;

/// <summary>ظاهر مؤثر Store را می‌خواند و cache می‌کند.</summary>
public interface IStoreAppearanceProjector
{
    /// <summary>ظاهر مؤثر همین Store را برمی‌گرداند.</summary>
    Task<StoreAppearanceProjection> GetEffectiveAsync(CancellationToken cancellationToken);

    /// <summary>ظاهر را برای یک زمینهٔ صریح می‌خواند.</summary>
    Task<StoreAppearanceProjection> GetEffectiveAsync(
        CommerceContext? context,
        CancellationToken cancellationToken);

    /// <summary>پس از تغییر تنظیمات Store، cache همان scope باطل می‌شود.</summary>
    void Invalidate(CommerceContext? context);
}
