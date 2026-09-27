using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;

namespace Tooba.Catalog.Application.Settings;

/// <summary>Maps quantity rounding mode to the Admin view shape.</summary>
internal static class StoreQuantitySettingsViews
{
    public static StoreQuantitySettingsView ToView(QuantityRoundingMode mode) => mode switch
    {
        QuantityRoundingMode.Floor => new("Floor", "رو به پایین", "Floor"),
        QuantityRoundingMode.Ceiling => new("Ceiling", "رو به بالا", "Ceiling"),
        _ => new("Nearest", "نزدیک‌ترین مقدار", "Nearest"),
    };
}

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
