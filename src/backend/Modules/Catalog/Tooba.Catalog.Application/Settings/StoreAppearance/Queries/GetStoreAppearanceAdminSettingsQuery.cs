using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Settings.StoreAppearance.Models;

namespace Tooba.Catalog.Application.Settings.StoreAppearance.Queries;

/// <summary>Admin GET store-appearance settings aggregate.</summary>
public sealed record GetStoreAppearanceAdminSettingsQuery : IRequest<Result<StoreAppearanceAdminView>>;
