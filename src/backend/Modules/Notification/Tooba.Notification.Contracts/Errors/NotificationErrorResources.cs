using System.Globalization;
using System.Resources;
using Tooba.BuildingBlocks.Localization;

namespace Tooba.Notification.Contracts.Errors;

/// <summary>Resource manager marker for NotificationErrors.resx.</summary>
public static class NotificationErrorResources
{
    /// <summary>ResourceManager for Notification error resources.</summary>
    public static ResourceManager Manager { get; } =
        new("Tooba.Notification.Contracts.Resources.NotificationErrors", typeof(NotificationErrorResources).Assembly);
}

/// <summary>Notification-owned error resource set for the <c>notification.</c> key space.</summary>
public sealed class NotificationErrorResourceSet : IErrorResourceSet
{
    /// <inheritdoc />
    public bool Owns(string localizationKey) =>
        localizationKey.StartsWith("notification.", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? GetString(string localizationKey, CultureInfo culture) =>
        NotificationErrorResources.Manager.GetString(localizationKey, culture);
}
