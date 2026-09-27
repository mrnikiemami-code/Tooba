using System.Globalization;
using System.Resources;
using Tooba.BuildingBlocks.Localization;

namespace Tooba.Content.Endpoints.Resources;

/// <summary>نشانگر منبع خطاهای Content.</summary>
public static class ContentErrorResources
{
    /// <summary>ResourceManager برای ContentErrors.resx.</summary>
    public static ResourceManager Manager { get; } =
        new("Tooba.Content.Endpoints.Resources.ContentErrors", typeof(ContentErrorResources).Assembly);
}

/// <summary>مجموعهٔ منبع Content — مالکیت محدود به فضای content.*</summary>
public sealed class ContentErrorResourceSet : IErrorResourceSet
{
    /// <inheritdoc />
    public bool Owns(string localizationKey) =>
        localizationKey.StartsWith("content.", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? GetString(string localizationKey, CultureInfo culture) =>
        ContentErrorResources.Manager.GetString(localizationKey, culture);
}
