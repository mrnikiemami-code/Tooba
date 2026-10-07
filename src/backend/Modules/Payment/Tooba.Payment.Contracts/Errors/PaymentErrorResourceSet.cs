using System.Globalization;
using System.Resources;
using Tooba.BuildingBlocks.Localization;

namespace Tooba.Payment.Contracts.Errors;

/// <summary>Resource manager marker for PaymentErrors.resx.</summary>
public static class PaymentErrorResources
{
    /// <summary>ResourceManager for the Payment bilingual error resources.</summary>
    public static ResourceManager Manager { get; } =
        new("Tooba.Payment.Contracts.Resources.PaymentErrors", typeof(PaymentErrorResources).Assembly);
}

/// <summary>
/// Payment-owned error resource set for the <c>payment.</c> and <c>admin.payment.</c> keyspaces.
/// Payment owns the user-facing text for its own stable codes so the module can be extracted as an
/// isolated microservice without leaving its copy behind in another module.
/// </summary>
public sealed class PaymentErrorResourceSet : IErrorResourceSet
{
    /// <inheritdoc />
    public bool Owns(string localizationKey) =>
        localizationKey.StartsWith("payment.", StringComparison.OrdinalIgnoreCase)
        || localizationKey.StartsWith("admin.payment.", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? GetString(string localizationKey, CultureInfo culture) =>
        PaymentErrorResources.Manager.GetString(localizationKey, culture);
}
