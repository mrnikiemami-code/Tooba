using Tooba.Content.Application;

namespace Tooba.Host.Content;

/// <summary>ترکیب HTTP برای رسانهٔ مقاله.</summary>
public sealed class ContentArticleMediaPanelComposer
{
    private readonly IContentArticleMediaDirectory _media;

    /// <summary>دایرکتوری رسانهٔ مقاله را تزریق می‌کند.</summary>
    public ContentArticleMediaPanelComposer(IContentArticleMediaDirectory media) => _media = media;

    /// <summary>workspace رسانه را برمی‌گرداند.</summary>
    public Task<ArticleMediaWorkspaceDto> GetWorkspaceAsync(Guid articleId, CancellationToken cancellationToken) =>
        _media.GetWorkspaceAsync(articleId, cancellationToken);

    /// <summary>تصویر شاخص را تنظیم می‌کند.</summary>
    public Task<ArticleMediaWorkspaceDto> AssignFeaturedAsync(
        Guid articleId,
        Guid? mediaAssetId,
        CancellationToken cancellationToken) =>
        _media.AssignFeaturedAsync(articleId, mediaAssetId, cancellationToken);

    /// <summary>تصویر SEO را تنظیم می‌کند.</summary>
    public Task<ArticleMediaWorkspaceDto> AssignSeoImageAsync(
        Guid articleId,
        Guid? mediaAssetId,
        CancellationToken cancellationToken) =>
        _media.AssignSeoImageAsync(articleId, mediaAssetId, cancellationToken);

    /// <summary>به گالری اضافه می‌کند.</summary>
    public Task<ArticleMediaWorkspaceDto> AddGalleryAsync(
        Guid articleId,
        IReadOnlyList<Guid> mediaAssetIds,
        CancellationToken cancellationToken) =>
        _media.AddGalleryItemsAsync(articleId, mediaAssetIds, cancellationToken);

    /// <summary>از گالری حذف می‌کند.</summary>
    public Task<ArticleMediaWorkspaceDto> RemoveGalleryAsync(
        Guid articleId,
        Guid mediaAssetId,
        CancellationToken cancellationToken) =>
        _media.RemoveGalleryItemAsync(articleId, mediaAssetId, cancellationToken);

    /// <summary>گالری را مرتب می‌کند.</summary>
    public Task<ArticleMediaWorkspaceDto> ReorderGalleryAsync(
        Guid articleId,
        IReadOnlyList<Guid> orderedMediaAssetIds,
        CancellationToken cancellationToken) =>
        _media.ReorderGalleryAsync(articleId, orderedMediaAssetIds, cancellationToken);

    /// <summary>متادیتای گالری را به‌روزرسانی می‌کند.</summary>
    public Task<ArticleMediaWorkspaceDto> PatchGalleryAsync(
        Guid articleId,
        Guid mediaAssetId,
        string? altText,
        string? caption,
        CancellationToken cancellationToken) =>
        _media.PatchGalleryItemAsync(articleId, mediaAssetId, altText, caption, cancellationToken);
}
