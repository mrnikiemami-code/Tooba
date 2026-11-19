namespace Tooba.Story.Application.Stories.Ports;

/// <summary>صفحهٔ گزینه‌های محصول برای CTA استوری (جستجو/صفحه از همان مجموعهٔ واجد شرایط).</summary>
public sealed record StoryProductOptionsPage(
    IReadOnlyList<StoryProductOption> Items,
    int TotalCount);

/// <summary>ردیف‌های انتخاب محصول برای CTA استوری (پیشنهاد فروشنده یا محصولات منتشرشدهٔ ویترین).</summary>
public interface IStoryProductOptionsPort
{
    /// <summary>
    /// جستجو روی پیشنهادهای واقعی فروشگاه (نه برنده‌ی کارت)، سپس صفحه و شمارش از همان مجموعه.
    /// </summary>
    Task<StoryProductOptionsPage> SearchForSellerAsync(
        Guid sellerPartyId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>
    /// جستجو روی محصولات Published ویترین اصلی — بدون sampler توسعه.
    /// </summary>
    Task<StoryProductOptionsPage> SearchPublishedAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}

/// <summary>ردیف انتخاب محصول برای CTA استوری.</summary>
public sealed record StoryProductOption(Guid ProductId, string Title);
