namespace Tooba.Content.Application;

/// <summary>ردیف گالری رسانهٔ مقاله.</summary>
public sealed record ArticleGalleryItemDto(
    Guid MediaAssetId,
    int DisplayOrder,
    string? AltText,
    string? Caption);

/// <summary>workspace رسانهٔ مقاله برای Admin.</summary>
public sealed record ArticleMediaWorkspaceDto(
    Guid ArticleId,
    Guid? FeaturedMediaAssetId,
    Guid? SeoImageMediaAssetId,
    Guid? EffectiveSeoImageMediaAssetId,
    IReadOnlyList<ArticleGalleryItemDto> Gallery);

/// <summary>مدیریت ارجاع‌های ساختاریافتهٔ رسانهٔ مقاله به DAM.</summary>
public interface IContentArticleMediaDirectory
{
    /// <summary>workspace رسانه را برمی‌گرداند.</summary>
    Task<ArticleMediaWorkspaceDto> GetWorkspaceAsync(Guid articleId, CancellationToken cancellationToken);

    /// <summary>تصویر شاخص را تنظیم یا unassign می‌کند.</summary>
    Task<ArticleMediaWorkspaceDto> AssignFeaturedAsync(
        Guid articleId,
        Guid? mediaAssetId,
        CancellationToken cancellationToken);

    /// <summary>تصویر SEO را تنظیم یا unassign می‌کند.</summary>
    Task<ArticleMediaWorkspaceDto> AssignSeoImageAsync(
        Guid articleId,
        Guid? mediaAssetId,
        CancellationToken cancellationToken);

    /// <summary>دارایی‌ها را به گالری اضافه می‌کند.</summary>
    Task<ArticleMediaWorkspaceDto> AddGalleryItemsAsync(
        Guid articleId,
        IReadOnlyList<Guid> mediaAssetIds,
        CancellationToken cancellationToken);

    /// <summary>ردیف گالری را حذف می‌کند (unassign).</summary>
    Task<ArticleMediaWorkspaceDto> RemoveGalleryItemAsync(
        Guid articleId,
        Guid mediaAssetId,
        CancellationToken cancellationToken);

    /// <summary>ترتیب گالری را بازنویسی می‌کند.</summary>
    Task<ArticleMediaWorkspaceDto> ReorderGalleryAsync(
        Guid articleId,
        IReadOnlyList<Guid> orderedMediaAssetIds,
        CancellationToken cancellationToken);

    /// <summary>متادیتای سطح استفادهٔ گالری را به‌روزرسانی می‌کند.</summary>
    Task<ArticleMediaWorkspaceDto> PatchGalleryItemAsync(
        Guid articleId,
        Guid mediaAssetId,
        string? altText,
        string? caption,
        CancellationToken cancellationToken);

    /// <summary>تعداد ارجاع‌های ساختاریافته (featured/seo/gallery) به یک دارایی.</summary>
    Task<int> CountStructuredReferencesAsync(Guid mediaAssetId, CancellationToken cancellationToken);
}
