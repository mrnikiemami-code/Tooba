using System.Globalization;
using System.Resources;
using Tooba.BuildingBlocks.Localization;

namespace Tooba.UserPreference.Endpoints.Resources;

/// <summary>نشانگر منبع خطاهای UserPreference.</summary>
public static class UserPreferenceErrorResources
{
    /// <summary>ResourceManager.</summary>
    public static ResourceManager Manager { get; } =
        new("Tooba.UserPreference.Endpoints.Resources.UserPreferenceErrors", typeof(UserPreferenceErrorResources).Assembly);
}

/// <summary>مجموعهٔ منبع UserPreference.</summary>
public sealed class UserPreferenceErrorResourceSet : IErrorResourceSet
{
    /// <inheritdoc />
    public bool Owns(string localizationKey) =>
        localizationKey.StartsWith("preference.", StringComparison.OrdinalIgnoreCase)
        || localizationKey.StartsWith("ui_preference.", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? GetString(string localizationKey, CultureInfo culture) =>
        UserPreferenceErrorResources.Manager.GetString(localizationKey, culture);
}
