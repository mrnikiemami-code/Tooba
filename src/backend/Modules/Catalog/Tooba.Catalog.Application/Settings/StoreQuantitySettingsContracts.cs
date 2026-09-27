using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;

namespace Tooba.Catalog.Application.Settings;

/// <summary>Catalog-owned port for store quantity rounding settings.</summary>
public interface IStoreQuantitySettingsDirectory
{
    /// <summary>Reads the effective global rounding mode (Nearest when row missing).</summary>
    Task<QuantityRoundingMode> GetAsync(CancellationToken cancellationToken);

    /// <summary>Writes the global rounding mode and returns the canonical value.</summary>
    Task<Result<QuantityRoundingMode>> SaveAsync(string? globalRoundingMode, CancellationToken cancellationToken);
}

/// <summary>Admin read model for quantity-rounding settings (success JSON shape).</summary>
public sealed record StoreQuantitySettingsView(
    string GlobalRoundingMode,
    string LabelFa,
    string LabelEn);

/// <summary>GET /v1/admin/settings/quantity-rounding</summary>
public sealed record GetStoreQuantitySettingsQuery : IRequest<Result<StoreQuantitySettingsView>>;

/// <summary>PUT /v1/admin/settings/quantity-rounding</summary>
/// <param name="GlobalRoundingMode">Floor / Ceiling / Nearest.</param>
public sealed record SaveStoreQuantitySettingsCommand(string? GlobalRoundingMode)
    : IRequest<Result<StoreQuantitySettingsView>>;
