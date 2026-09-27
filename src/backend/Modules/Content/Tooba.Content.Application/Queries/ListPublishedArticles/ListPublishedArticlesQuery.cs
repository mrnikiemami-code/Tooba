using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Models;
using Tooba.Content.Application.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Domain.Aggregates;
using Tooba.Content.Domain.Rules;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Queries.ListPublishedArticles;

public sealed record ListPublishedArticlesQuery(
    int Page, int PageSize, string? Category, string? Locale, string? CategorySlug, string? AuthorSlug)
    : IRequest<Result<PagedResult<PublishedArticleItem>>>;

public sealed class ListPublishedArticlesQueryHandler(
    IContentDirectory content,
    IContentCategoryDirectory categories,
    IContentAuthorDirectory authors)
    : IRequestHandler<ListPublishedArticlesQuery, Result<PagedResult<PublishedArticleItem>>>
{
    public async Task<Result<PagedResult<PublishedArticleItem>>> Handle(
        ListPublishedArticlesQuery request, CancellationToken cancellationToken)
    {
        return await ContentOperation.ExecuteAsync(async () =>
        {
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, 50);
            var filterLocale = string.IsNullOrWhiteSpace(request.Locale)
                ? null
                : ContentTaxonomySeoRules.ResolveContentLocale(request.Locale);
            var taxonomyLocale = ContentTaxonomySeoRules.ResolveContentLocale(request.Locale);

            Guid? categoryId = null;
            if (!string.IsNullOrWhiteSpace(request.CategorySlug))
            {
                var publicCategory = await categories.GetPublicBySlugAsync(taxonomyLocale, request.CategorySlug, cancellationToken);
                if (publicCategory is null)
                    return new PagedResult<PublishedArticleItem>([], page, pageSize, 0);
                categoryId = publicCategory.CategoryId;
            }

            Guid? authorId = null;
            if (!string.IsNullOrWhiteSpace(request.AuthorSlug))
            {
                var publicAuthor = await authors.GetPublicBySlugAsync(request.AuthorSlug, taxonomyLocale, cancellationToken);
                if (publicAuthor is null)
                    return new PagedResult<PublishedArticleItem>([], page, pageSize, 0);
                authorId = publicAuthor.AuthorId;
            }

            return await content.ListPublishedAsync(page, pageSize, request.Category, filterLocale, categoryId, authorId, cancellationToken);
        }, ContentErrorCodes.UpdateRejected);
    }
}
