using System.Globalization;
using System.Resources;
using Tooba.BuildingBlocks.Localization;

namespace Tooba.Wallet.Endpoints.Resources;

/// <summary>نشانگر منبع خطاهای Wallet Endpoints.</summary>
public static class WalletErrorResources
{
    /// <summary>ResourceManager برای WalletErrors.resx.</summary>
    public static ResourceManager Manager { get; } =
        new("Tooba.Wallet.Endpoints.Resources.WalletErrors", typeof(WalletErrorResources).Assembly);
}

/// <summary>مجموعهٔ منبع Wallet — مالک کلیدهای wallet.*.</summary>
public sealed class WalletErrorResourceSet : IErrorResourceSet
{
    /// <inheritdoc />
    public bool Owns(string localizationKey) =>
        localizationKey.StartsWith("wallet.", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? GetString(string localizationKey, CultureInfo culture) =>
        WalletErrorResources.Manager.GetString(localizationKey, culture);
}
