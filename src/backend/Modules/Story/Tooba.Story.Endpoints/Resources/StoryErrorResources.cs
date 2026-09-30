using System.Globalization;
using System.Resources;
using Tooba.BuildingBlocks.Localization;

namespace Tooba.Story.Endpoints.Resources;

/// <summary>نشانگر منبع خطاهای Story.</summary>
public static class StoryErrorResources
{
    public static ResourceManager Manager { get; } =
        new("Tooba.Story.Endpoints.Resources.StoryErrors", typeof(StoryErrorResources).Assembly);
}

/// <summary>مجموعهٔ منبع Story — مالکیت <c>story.</c>.</summary>
public sealed class StoryErrorResourceSet : IErrorResourceSet
{
    /// <inheritdoc />
    public bool Owns(string localizationKey) =>
        localizationKey.StartsWith("story.", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? GetString(string localizationKey, CultureInfo culture) =>
        StoryErrorResources.Manager.GetString(localizationKey, culture);
}
