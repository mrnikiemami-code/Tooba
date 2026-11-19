namespace Tooba.Story.Application.Stories.Ports;

/// <summary>ردیف‌های انتخاب محصول برای CTA استوری (پیشنهاد فروشنده یا محصولات منتشرشدهٔ ویترین).</summary>
public interface IStoryProductOptionsPort
{
    /// <summary>محصول‌های واجد شرایط یک فروشگاه بر اساس پیشنهاد واقعی، نه برنده‌ی کارت ویترین.</summary>
    Task<IReadOnlyList<StoryProductOption>> ListForSellerAsync(
        Guid sellerPartyId,
        CancellationToken cancellationToken);

    /// <summary>محصول‌های منتشرشده برای CTA استوری ویترین اصلی (بدون فروشگاه).</summary>
    Task<IReadOnlyList<StoryProductOption>> ListPublishedAsync(CancellationToken cancellationToken);
}

/// <summary>ردیف انتخاب محصول برای CTA استوری.</summary>
public sealed record StoryProductOption(Guid ProductId, string Title);
