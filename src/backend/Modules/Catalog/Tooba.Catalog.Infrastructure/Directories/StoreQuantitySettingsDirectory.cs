using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Settings.Quantity.Ports;
using Tooba.Catalog.Contracts.Errors;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure.Directories;

/// <summary>Catalog persistence for store quantity rounding settings.</summary>
public sealed class StoreQuantitySettingsDirectory : IStoreQuantitySettingsDirectory
{
    private readonly CatalogDbContext _catalog;
    private readonly IClock _clock;

    /// <summary>Creates the directory.</summary>
    public StoreQuantitySettingsDirectory(CatalogDbContext catalog, IClock clock)
    {
        _catalog = catalog;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<QuantityRoundingMode> GetAsync(CancellationToken cancellationToken)
    {
        var settings = await _catalog.StoreQuantitySettings.AsNoTracking()
            .SingleOrDefaultAsync(s => s.SettingsId == StoreQuantitySettings.SingletonId, cancellationToken);
        return settings?.RoundingMode ?? QuantityRoundingMode.Nearest;
    }

    /// <inheritdoc />
    public async Task<Result<QuantityRoundingMode>> SaveAsync(string? globalRoundingMode, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<QuantityRoundingMode>(globalRoundingMode, ignoreCase: true, out var mode)
            || mode is not (QuantityRoundingMode.Floor or QuantityRoundingMode.Ceiling or QuantityRoundingMode.Nearest))
        {
            return Result.Failure<QuantityRoundingMode>(new SemanticError(CatalogErrorCodes.QuantityRoundingInvalid));
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
        return Result.Success(mode);
    }
}
