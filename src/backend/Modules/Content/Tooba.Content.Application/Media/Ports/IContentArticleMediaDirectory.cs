using Tooba.Content.Application.Media.Models;

namespace Tooba.Content.Application.Media.Ports;

/// <summary>مدیریت ارجاع‌های ساختاریافتهٔ رسانهٔ مقاله به DAM.</summary>
public interface IContentArticleMediaDirectory
{
    Task<ArticleMediaWorkspaceDto> GetWorkspaceAsync(Guid articleId, CancellationToken cancellationToken);

    Task<ArticleMediaWorkspaceDto> AssignFeaturedAsync(
        Guid articleId, Guid? mediaAssetId, CancellationToken cancellationToken);

    Task<ArticleMediaWorkspaceDto> AssignSeoImageAsync(
        Guid articleId, Guid? mediaAssetId, CancellationToken cancellationToken);

    Task<ArticleMediaWorkspaceDto> AddGalleryItemsAsync(
        Guid articleId, IReadOnlyList<Guid> mediaAssetIds, CancellationToken cancellationToken);

    Task<ArticleMediaWorkspaceDto> RemoveGalleryItemAsync(
        Guid articleId, Guid mediaAssetId, CancellationToken cancellationToken);

    Task<ArticleMediaWorkspaceDto> ReorderGalleryAsync(
        Guid articleId, IReadOnlyList<Guid> orderedMediaAssetIds, CancellationToken cancellationToken);

    Task<ArticleMediaWorkspaceDto> PatchGalleryItemAsync(
        Guid articleId, Guid mediaAssetId, string? altText, string? caption, CancellationToken cancellationToken);

    Task<int> CountStructuredReferencesAsync(Guid mediaAssetId, CancellationToken cancellationToken);
}
