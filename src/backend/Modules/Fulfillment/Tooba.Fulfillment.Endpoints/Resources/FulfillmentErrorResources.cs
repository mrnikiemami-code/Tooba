using System.Globalization;
using System.Resources;
using Tooba.BuildingBlocks.Localization;

namespace Tooba.Fulfillment.Endpoints.Resources;

/// <summary>نشانگر منبع خطاهای Fulfillment.</summary>
public static class FulfillmentErrorResources
{
    /// <summary>ResourceManager برای FulfillmentErrors.resx.</summary>
    public static ResourceManager Manager { get; } =
        new("Tooba.Fulfillment.Endpoints.Resources.FulfillmentErrors", typeof(FulfillmentErrorResources).Assembly);
}

/// <summary>مجموعهٔ منبع Fulfillment — مالکیت صریح کلید، بدون شاخهٔ en/fa دستی.</summary>
public sealed class FulfillmentErrorResourceSet : IErrorResourceSet
{
    /// <inheritdoc />
    public bool Owns(string localizationKey) =>
        localizationKey.StartsWith("fulfillment.", StringComparison.OrdinalIgnoreCase)
        || localizationKey.StartsWith("shipping_service", StringComparison.OrdinalIgnoreCase)
        || localizationKey.Equals("seller.order.handle.denied", StringComparison.OrdinalIgnoreCase)
        || localizationKey.Equals("seller.order.handle.scope_denied", StringComparison.OrdinalIgnoreCase)
        || localizationKey.Equals("customer.actor.missing", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? GetString(string localizationKey, CultureInfo culture) =>
        FulfillmentErrorResources.Manager.GetString(localizationKey, culture);
}
