using System.Globalization;
using System.Resources;
using Tooba.BuildingBlocks.Localization;

namespace Tooba.BulkInquiry.Contracts.Errors;

/// <summary>Resource manager marker for BulkInquiryErrors.resx.</summary>
public static class BulkInquiryErrorResources
{
    /// <summary>ResourceManager for BulkInquiry error resources.</summary>
    public static ResourceManager Manager { get; } =
        new("Tooba.BulkInquiry.Contracts.Resources.BulkInquiryErrors", typeof(BulkInquiryErrorResources).Assembly);
}

/// <summary>BulkInquiry-owned error resource set for the <c>bulk_inquiry.</c> key space.</summary>
public sealed class BulkInquiryErrorResourceSet : IErrorResourceSet
{
    /// <inheritdoc />
    public bool Owns(string localizationKey) =>
        localizationKey.StartsWith("bulk_inquiry.", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? GetString(string localizationKey, CultureInfo culture) =>
        BulkInquiryErrorResources.Manager.GetString(localizationKey, culture);
}
