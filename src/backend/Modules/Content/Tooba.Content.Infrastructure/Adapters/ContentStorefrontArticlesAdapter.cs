using Tooba.Content.Application.Articles.Ports;
using Tooba.Content.Contracts.Storefront;
using Tooba.Content.Domain.Rules;

namespace Tooba.Content.Infrastructure.Adapters;

/// <summary>Adapts Content application directory to Contracts storefront articles port.</summary>
public sealed class ContentStorefrontArticlesAdapter : IContentStorefrontArticlesPort
{
    private readonly IContentDirectory _content;

    /// <summary>Creates the adapter.</summary>
    public ContentStorefrontArticlesAdapter(IContentDirectory content) => _content = content;

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContentStorefrontArticleDto>> ListPublishedForHomeAsync(
        int limit,
        string? locale,
        CancellationToken cancellationToken)
    {
        var contentLocale = ContentTaxonomySeoRules.ResolveContentLocale(locale);
        var articles = await _content.ListPublishedForHomeAsync(limit, contentLocale, cancellationToken);
        return articles.Select(a => new ContentStorefrontArticleDto(
            a.ArticleId,
            a.Slug,
            a.Title,
            a.Excerpt,
            a.CoverMediaAssetId,
            a.PublishDate,
            a.AuthorDisplayName,
            a.Tags,
            a.IsFeatured)).ToList();
    }
}
