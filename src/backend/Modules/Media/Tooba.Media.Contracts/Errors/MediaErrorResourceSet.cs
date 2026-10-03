using System.Globalization;
using System.Resources;
using Tooba.BuildingBlocks.Localization;

namespace Tooba.Media.Contracts.Errors;

/// <summary>Resource manager marker for MediaErrors.resx.</summary>
public static class MediaErrorResources
{
    /// <summary>ResourceManager for Media error resources.</summary>
    public static ResourceManager Manager { get; } =
        new("Tooba.Media.Contracts.Resources.MediaErrors", typeof(MediaErrorResources).Assembly);
}

/// <summary>Media-owned error resource set for the <c>media.</c> key space.</summary>
public sealed class MediaErrorResourceSet : IErrorResourceSet
{
    /// <inheritdoc />
    public bool Owns(string localizationKey) =>
        localizationKey.StartsWith("media.", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? GetString(string localizationKey, CultureInfo culture) =>
        MediaErrorResources.Manager.GetString(localizationKey, culture);
}
