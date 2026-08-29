namespace Tooba.Catalog.Application;

/// <summary>
/// کمک به seed/dev برای تکمیل حداقل SEO قبل از Publish با دروازهٔ آمادگی تجمیعی.
/// </summary>
public static class ProductPublishPrep
{
    /// <summary>
    /// اگر SEO آماده نیست، توضیح فارسی حداقلی (و حفظ slug/title موجود) می‌نویسد.
    /// </summary>
    public static async Task EnsureMinimalSeoForPublishAsync(
        ICatalogDirectory catalog,
        Guid productId,
        string descriptionFa,
        CancellationToken cancellationToken)
    {
        var detail = await catalog.GetProductSeoAsync(productId, "fa-IR", cancellationToken);
        if (detail.Readiness.IsReady)
        {
            return;
        }

        var description = string.IsNullOrWhiteSpace(detail.SeoDescription)
            ? descriptionFa
            : detail.SeoDescription;
        await catalog.UpdateProductSeoAsync(
            productId,
            new ProductSeoUpdateInput(
                "fa-IR",
                detail.Slug,
                detail.SeoTitle,
                description,
                detail.UpdatedAt),
            cancellationToken);
    }
}
