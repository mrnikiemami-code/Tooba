using System.Globalization;
using System.Resources;
using Tooba.BuildingBlocks.Localization;

namespace Tooba.Reviews.Endpoints.Resources;

/// <summary>نشانگر منبع خطاهای Reviews.</summary>
public static class ReviewsErrorResources
{
    public static ResourceManager Manager { get; } =
        new("Tooba.Reviews.Endpoints.Resources.ReviewsErrors", typeof(ReviewsErrorResources).Assembly);
}

/// <summary>مجموعهٔ منبع Reviews — مالکیت <c>reviews.</c>.</summary>
public sealed class ReviewsErrorResourceSet : IErrorResourceSet
{
    /// <inheritdoc />
    public bool Owns(string localizationKey) =>
        localizationKey.StartsWith("reviews.", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? GetString(string localizationKey, CultureInfo culture) =>
        ReviewsErrorResources.Manager.GetString(localizationKey, culture);
}
