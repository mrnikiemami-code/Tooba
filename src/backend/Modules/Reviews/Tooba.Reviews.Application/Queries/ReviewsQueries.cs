using MediatR;
using Tooba.BuildingBlocks.Grid;
using Tooba.Reviews.Application.Models;
using Tooba.Reviews.Application.Presentation;

namespace Tooba.Reviews.Application.Queries;

/// <summary>صفحهٔ عمومی نظرات Published یک محصول.</summary>
public sealed record GetPublishedReviewsQuery(string Slug, int Page, int PageSize)
    : IRequest<PublicReviewsResponse?>;

/// <summary>Handler صفحهٔ عمومی.</summary>
public sealed class GetPublishedReviewsQueryHandler(ReviewsPresentationComposer composer)
    : IRequestHandler<GetPublishedReviewsQuery, PublicReviewsResponse?>
{
    /// <inheritdoc />
    public Task<PublicReviewsResponse?> Handle(GetPublishedReviewsQuery request, CancellationToken cancellationToken)
        => composer.GetPublishedAsync(request.Slug, request.Page, request.PageSize, cancellationToken);
}

/// <summary>فهرست نظرات فروشنده.</summary>
public sealed record ListSellerReviewsQuery(Guid SellerPartyId, string? Status, int Page, int PageSize)
    : IRequest<SellerReviewsResponse>;

/// <summary>Handler فهرست فروشنده.</summary>
public sealed class ListSellerReviewsQueryHandler(ReviewsPresentationComposer composer)
    : IRequestHandler<ListSellerReviewsQuery, SellerReviewsResponse>
{
    /// <inheritdoc />
    public Task<SellerReviewsResponse> Handle(ListSellerReviewsQuery request, CancellationToken cancellationToken)
        => composer.ListSellerAsync(request.SellerPartyId, request.Status, request.Page, request.PageSize, cancellationToken);
}

/// <summary>صف Pending مدیر.</summary>
public sealed record ListPendingAdminReviewsQuery(int Page, int PageSize) : IRequest<AdminReviewsResponse>;

/// <summary>Handler صف Pending.</summary>
public sealed class ListPendingAdminReviewsQueryHandler(ReviewsPresentationComposer composer)
    : IRequestHandler<ListPendingAdminReviewsQuery, AdminReviewsResponse>
{
    /// <inheritdoc />
    public Task<AdminReviewsResponse> Handle(ListPendingAdminReviewsQuery request, CancellationToken cancellationToken)
        => composer.GetPendingAsync(request.Page, request.PageSize, cancellationToken);
}

/// <summary>گرید Pending مدیر.</summary>
public sealed record QueryPendingAdminReviewGridQuery(GridQueryRequest Request)
    : IRequest<GridPageResponse<AdminReviewItem>>;

/// <summary>Handler گرید Pending.</summary>
public sealed class QueryPendingAdminReviewGridQueryHandler(ReviewsPresentationComposer composer)
    : IRequestHandler<QueryPendingAdminReviewGridQuery, GridPageResponse<AdminReviewItem>>
{
    /// <inheritdoc />
    public Task<GridPageResponse<AdminReviewItem>> Handle(
        QueryPendingAdminReviewGridQuery request,
        CancellationToken cancellationToken)
        => composer.QueryPendingGridAsync(request.Request, cancellationToken);
}
