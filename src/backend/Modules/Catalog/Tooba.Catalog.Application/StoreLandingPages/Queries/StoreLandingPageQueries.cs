using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.StoreLandingPages.Models;

namespace Tooba.Catalog.Application.StoreLandingPages.Queries;

/// <summary>فهرست صفحات Admin.</summary>
public sealed record ListStoreLandingPagesQuery : IRequest<Result<IReadOnlyList<StoreLandingPageAdminView>>>;

/// <summary>خواندن یک صفحه Admin.</summary>
public sealed record GetStoreLandingPageQuery(Guid PageId) : IRequest<Result<StoreLandingPageAdminView>>;

/// <summary>پیش‌نمایش Admin.</summary>
public sealed record PreviewStoreLandingPageQuery(Guid PageId) : IRequest<Result<StoreLandingPagePublicView>>;

/// <summary>فهرست بخش‌ها.</summary>
public sealed record ListStoreLandingPageSectionsQuery(Guid PageId)
    : IRequest<Result<IReadOnlyList<StoreLandingPageSectionAdminView>>>;

/// <summary>انتخاب خانه Admin/Storefront.</summary>
public sealed record GetStoreHomeSelectionQuery : IRequest<Result<StoreHomeSelectionView>>;

/// <summary>صفحهٔ عمومی با locale+slug.</summary>
public sealed record ResolvePublicStoreLandingPageQuery(string? Locale, string? Slug)
    : IRequest<Result<StoreLandingPagePublicView>>;

/// <summary>sitemap ایندکس‌پذیر.</summary>
public sealed record ListStoreLandingSitemapQuery
    : IRequest<Result<IReadOnlyList<StoreLandingSitemapEntry>>>;
