using System.Globalization;
using System.Resources;
using Tooba.CustomerProfile.Application.Ports;

namespace Tooba.CustomerProfile.Endpoints.Resources;

/// <summary>Marker for customer-account presentation ResourceManager.</summary>
public static class CustomerAccountPresentationResources
{
    /// <summary>ResourceManager for CustomerAccountPresentation.resx.</summary>
    public static ResourceManager Manager { get; } =
        new(
            "Tooba.CustomerProfile.Endpoints.Resources.CustomerAccountPresentation",
            typeof(CustomerAccountPresentationResources).Assembly);
}

/// <summary>Endpoints-owned display texts sourced from presentation resources (no inline literals).</summary>
public sealed class CustomerAccountDisplayTexts : ICustomerAccountDisplayTexts
{
    private const string DefaultDisplayNameKey = "customer.account.default_display_name";

    /// <inheritdoc />
    public string DefaultDisplayName =>
        CustomerAccountPresentationResources.Manager.GetString(
            DefaultDisplayNameKey,
            CultureInfo.CurrentUICulture)
        ?? CustomerAccountPresentationResources.Manager.GetString(
            DefaultDisplayNameKey,
            CultureInfo.InvariantCulture)
        ?? DefaultDisplayNameKey;
}
