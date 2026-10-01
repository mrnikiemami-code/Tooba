using System.Globalization;
using System.Resources;
using Tooba.BuildingBlocks.Localization;

namespace Tooba.BulkInquiry.Endpoints.Resources;

/// <summary>نشانگر منبع خطاهای BulkInquiry.</summary>
public static class BulkInquiryErrorResources
{
    /// <summary>ResourceManager.</summary>
    public static ResourceManager Manager { get; } =
        new("Tooba.BulkInquiry.Endpoints.Resources.BulkInquiryErrors", typeof(BulkInquiryErrorResources).Assembly);
}

/// <summary>مجموعهٔ منبع BulkInquiry.</summary>
public sealed class BulkInquiryErrorResourceSet : IErrorResourceSet
{
    /// <inheritdoc />
    public bool Owns(string localizationKey) =>
        localizationKey.StartsWith("bulk_inquiry.", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? GetString(string localizationKey, CultureInfo culture) =>
        BulkInquiryErrorResources.Manager.GetString(localizationKey, culture);
}
