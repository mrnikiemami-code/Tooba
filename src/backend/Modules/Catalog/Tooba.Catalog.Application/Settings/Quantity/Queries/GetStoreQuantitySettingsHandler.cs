using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Settings.Quantity.Mapping;
using Tooba.Catalog.Application.Settings.Quantity.Models;
using Tooba.Catalog.Application.Settings.Quantity.Ports;

namespace Tooba.Catalog.Application.Settings.Quantity.Queries;

/// <summary>Query handler for Admin quantity-rounding settings.</summary>
public sealed class GetStoreQuantitySettingsHandler
    : IRequestHandler<GetStoreQuantitySettingsQuery, Result<StoreQuantitySettingsView>>
{
    private readonly IStoreQuantitySettingsDirectory _directory;

    /// <summary>Creates the handler.</summary>
    public GetStoreQuantitySettingsHandler(IStoreQuantitySettingsDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public async Task<Result<StoreQuantitySettingsView>> Handle(
        GetStoreQuantitySettingsQuery request,
        CancellationToken cancellationToken)
    {
        var mode = await _directory.GetAsync(cancellationToken);
        return Result.Success(StoreQuantitySettingsViews.ToView(mode));
    }
}
