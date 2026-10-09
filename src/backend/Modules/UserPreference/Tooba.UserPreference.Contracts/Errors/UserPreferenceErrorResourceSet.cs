using System.Globalization;
using System.Resources;
using Tooba.BuildingBlocks.Localization;

namespace Tooba.UserPreference.Contracts.Errors;

/// <summary>Resource manager marker for the UserPreference bilingual error resources.</summary>
public static class UserPreferenceErrorResources
{
    /// <summary>ResourceManager for UserPreferenceErrors.resx.</summary>
    public static ResourceManager Manager { get; } =
        new("Tooba.UserPreference.Contracts.Resources.UserPreferenceErrors", typeof(UserPreferenceErrorResources).Assembly);
}

/// <summary>
/// UserPreference-owned error resource set for the <c>preference.</c>, <c>ui_preference.</c> and
/// <c>user_preference.</c> keyspaces. UserPreference owns the user-facing text for its own stable
/// codes so the module can be extracted as an isolated microservice without leaving its copy behind
/// in another module. Locale selection stays in the central localizer.
/// </summary>
public sealed class UserPreferenceErrorResourceSet : IErrorResourceSet
{
    /// <inheritdoc />
    public bool Owns(string localizationKey) =>
        localizationKey.StartsWith("preference.", StringComparison.OrdinalIgnoreCase)
        || localizationKey.StartsWith("ui_preference.", StringComparison.OrdinalIgnoreCase)
        || localizationKey.StartsWith("user_preference.", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? GetString(string localizationKey, CultureInfo culture) =>
        UserPreferenceErrorResources.Manager.GetString(localizationKey, culture);
}
