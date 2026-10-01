using System.Globalization;
using System.Resources;
using Tooba.BuildingBlocks.Localization;

namespace Tooba.Support.Endpoints.Resources;

/// <summary>نشانگر منبع خطاهای Support Endpoints.</summary>
public static class SupportErrorResources
{
    /// <summary>ResourceManager برای SupportErrors.resx.</summary>
    public static ResourceManager Manager { get; } =
        new("Tooba.Support.Endpoints.Resources.SupportErrors", typeof(SupportErrorResources).Assembly);
}

/// <summary>مجموعهٔ منبع Support — مالک کلیدهای support.*.</summary>
public sealed class SupportErrorResourceSet : IErrorResourceSet
{
    /// <inheritdoc />
    public bool Owns(string localizationKey) =>
        localizationKey.StartsWith("support.", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? GetString(string localizationKey, CultureInfo culture) =>
        SupportErrorResources.Manager.GetString(localizationKey, culture);
}
