using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Settings.Quantity.Models;

namespace Tooba.Catalog.Application.Settings.Quantity.Commands;

/// <summary>PUT /v1/admin/settings/quantity-rounding</summary>
/// <param name="GlobalRoundingMode">Floor / Ceiling / Nearest.</param>
public sealed record SaveStoreQuantitySettingsCommand(string? GlobalRoundingMode)
    : IRequest<Result<StoreQuantitySettingsView>>;
