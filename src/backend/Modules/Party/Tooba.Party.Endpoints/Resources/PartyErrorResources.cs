using System.Globalization;
using System.Resources;
using Tooba.BuildingBlocks.Localization;

namespace Tooba.Party.Endpoints.Resources;

/// <summary>نشانگر منبع خطاهای Party.</summary>
public static class PartyErrorResources
{
    /// <summary>ResourceManager برای PartyErrors.resx.</summary>
    public static ResourceManager Manager { get; } =
        new("Tooba.Party.Endpoints.Resources.PartyErrors", typeof(PartyErrorResources).Assembly);
}

/// <summary>مجموعهٔ منبع Party — مالکیت کلیدهای <c>seller.settings.*</c> این ماژول.</summary>
public sealed class PartyErrorResourceSet : IErrorResourceSet
{
    /// <inheritdoc />
    public bool Owns(string localizationKey) =>
        localizationKey.StartsWith("seller.settings.", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? GetString(string localizationKey, CultureInfo culture) =>
        PartyErrorResources.Manager.GetString(localizationKey, culture);
}
