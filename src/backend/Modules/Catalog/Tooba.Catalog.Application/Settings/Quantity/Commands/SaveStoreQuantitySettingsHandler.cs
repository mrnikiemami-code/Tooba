using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Settings.Quantity.Mapping;
using Tooba.Catalog.Application.Settings.Quantity.Models;
using Tooba.Catalog.Application.Settings.Quantity.Ports;

namespace Tooba.Catalog.Application.Settings.Quantity.Commands;

/// <summary>Command handler for Admin quantity-rounding settings write.</summary>
public sealed class SaveStoreQuantitySettingsHandler
    : IRequestHandler<SaveStoreQuantitySettingsCommand, Result<StoreQuantitySettingsView>>
{
    private readonly IStoreQuantitySettingsDirectory _directory;

    /// <summary>Creates the handler.</summary>
    public SaveStoreQuantitySettingsHandler(IStoreQuantitySettingsDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public async Task<Result<StoreQuantitySettingsView>> Handle(
        SaveStoreQuantitySettingsCommand request,
        CancellationToken cancellationToken)
    {
        var saved = await _directory.SaveAsync(request.GlobalRoundingMode, cancellationToken);
        if (saved.IsFailure)
        {
            return Result.Failure<StoreQuantitySettingsView>(saved.Errors);
        }

        return Result.Success(StoreQuantitySettingsViews.ToView(saved.Value));
    }
}
