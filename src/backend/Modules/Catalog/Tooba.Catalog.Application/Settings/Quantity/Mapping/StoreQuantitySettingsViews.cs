using Tooba.BuildingBlocks;
using Tooba.Catalog.Application.Settings.Quantity.Models;

namespace Tooba.Catalog.Application.Settings.Quantity.Mapping;

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
