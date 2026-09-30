namespace Tooba.Content.Contracts.Storefront;

/// <summary>Published article item for storefront home / landing rails.</summary>
public sealed record ContentStorefrontArticleDto(
    Guid ArticleId,
    string Slug,
    string Title,
    string Excerpt,
    Guid? CoverMediaAssetId,
    DateTimeOffset PublishDate,
    string AuthorDisplayName,
    IReadOnlyList<string> Tags,
    bool IsFeatured);

/// <summary>
/// Narrow Contracts port for storefront article enrichment (locale resolved inside Content).
/// </summary>
public interface IContentStorefrontArticlesPort
{
    /// <summary>Lists recent published articles for home; resolves content locale from raw page locale.</summary>
    Task<IReadOnlyList<ContentStorefrontArticleDto>> ListPublishedForHomeAsync(
        int limit,
        string? locale,
        CancellationToken cancellationToken);
}
