using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure;

/// <summary>orchestration موقت نوشتن StoreQuantitySettings روی Catalog DbContext.</summary>
public sealed class StoreQuantitySettingsDirectory : IStoreQuantitySettingsDirectory
{
    private readonly CatalogDbContext _catalog;
    private readonly IClock _clock;

    /// <summary>دایرکتوری را به schema catalog وصل می‌کند.</summary>
    public StoreQuantitySettingsDirectory(CatalogDbContext catalog, IClock clock)
    {
        _catalog = catalog;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<QuantityRoundingMode> SaveAsync(string? globalRoundingMode, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<QuantityRoundingMode>(globalRoundingMode, ignoreCase: true, out var mode)
            || mode is not (QuantityRoundingMode.Floor or QuantityRoundingMode.Ceiling or QuantityRoundingMode.Nearest))
        {
            throw new PlatformHttpException(400, "حالت گرد کردن نامعتبر است.", "quantity.rounding.invalid");
        }

        var now = _clock.UtcNow;
        var row = await _catalog.StoreQuantitySettings
            .SingleOrDefaultAsync(x => x.SettingsId == StoreQuantitySettings.SingletonId, cancellationToken);
        if (row is null)
        {
            row = StoreQuantitySettings.CreateDefault(now);
            _catalog.StoreQuantitySettings.Add(row);
        }

        row.SetRoundingMode(mode, now);
        await _catalog.SaveChangesAsync(cancellationToken);
        return mode;
    }
}
