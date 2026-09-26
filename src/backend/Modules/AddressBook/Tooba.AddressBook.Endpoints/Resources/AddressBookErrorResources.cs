using System.Globalization;
using System.Resources;
using Tooba.BuildingBlocks.Localization;

namespace Tooba.AddressBook.Endpoints.Resources;

/// <summary>نشانگر منبع خطاهای AddressBook.</summary>
public static class AddressBookErrorResources
{
    /// <summary>ResourceManager برای AddressBookErrors.resx.</summary>
    public static ResourceManager Manager { get; } =
        new("Tooba.AddressBook.Endpoints.Resources.AddressBookErrors", typeof(AddressBookErrorResources).Assembly);
}

/// <summary>
/// مجموعهٔ منبع AddressBook — مالکیت محدود به فضای نشانی مشتری (<c>customer.address.</c>) تا با
/// مجموعه‌های ماژول‌های دیگر (مثل <c>customer.session.</c> در Order) تداخل نکند.
/// </summary>
public sealed class AddressBookErrorResourceSet : IErrorResourceSet
{
    /// <inheritdoc />
    public bool Owns(string localizationKey) =>
        localizationKey.StartsWith("customer.address.", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? GetString(string localizationKey, CultureInfo culture) =>
        AddressBookErrorResources.Manager.GetString(localizationKey, culture);
}
