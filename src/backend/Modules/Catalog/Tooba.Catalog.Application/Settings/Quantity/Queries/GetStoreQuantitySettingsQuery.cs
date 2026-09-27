using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Settings.Quantity.Models;

namespace Tooba.Catalog.Application.Settings.Quantity.Queries;

/// <summary>GET /v1/admin/settings/quantity-rounding</summary>
public sealed record GetStoreQuantitySettingsQuery : IRequest<Result<StoreQuantitySettingsView>>;
