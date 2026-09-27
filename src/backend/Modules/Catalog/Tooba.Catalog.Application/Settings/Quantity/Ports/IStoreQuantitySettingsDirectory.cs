using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;

namespace Tooba.Catalog.Application.Settings.Quantity.Ports;

/// <summary>Catalog-owned port for store quantity rounding settings.</summary>
public interface IStoreQuantitySettingsDirectory
{
    /// <summary>Reads the effective global rounding mode (Nearest when row missing).</summary>
    Task<QuantityRoundingMode> GetAsync(CancellationToken cancellationToken);

    /// <summary>Writes the global rounding mode and returns the canonical value.</summary>
    Task<Result<QuantityRoundingMode>> SaveAsync(string? globalRoundingMode, CancellationToken cancellationToken);
}
