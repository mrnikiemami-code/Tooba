using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Settings.StoreAppearance.Models;
using Tooba.Catalog.Application.Settings.StoreAppearance.Ports;

namespace Tooba.Catalog.Application.Settings.StoreAppearance.Queries;

/// <summary>Handler for <see cref="GetStoreAppearanceAdminSettingsQuery"/>.</summary>
public sealed class GetStoreAppearanceAdminSettingsHandler
    : IRequestHandler<GetStoreAppearanceAdminSettingsQuery, Result<StoreAppearanceAdminView>>
{
    private readonly IStoreAppearanceProjector _projector;

    /// <summary>Creates the handler.</summary>
    public GetStoreAppearanceAdminSettingsHandler(IStoreAppearanceProjector projector) => _projector = projector;

    /// <inheritdoc />
    public async Task<Result<StoreAppearanceAdminView>> Handle(
        GetStoreAppearanceAdminSettingsQuery request,
        CancellationToken cancellationToken)
    {
        var current = await _projector.GetEffectiveAsync(cancellationToken);
        return Result.Success(StoreAppearanceSettingsComposer.ToView(current));
    }
}
