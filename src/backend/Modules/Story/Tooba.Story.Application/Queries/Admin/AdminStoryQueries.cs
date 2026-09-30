using MediatR;
using Tooba.BuildingBlocks.Grid;
using Tooba.Story.Application.Presentation;
using Tooba.Story.Domain;

namespace Tooba.Story.Application.Queries.Admin;

/// <summary>فهرست مدیریتی استوری.</summary>
public sealed record ListAdminStoriesQuery(Guid TenantId, StoryReviewStatus? ReviewStatus, bool PendingReview)
    : IRequest<IReadOnlyList<AdminStorySnapshot>>;

/// <summary>Handler فهرست مدیریتی.</summary>
public sealed class ListAdminStoriesQueryHandler(StoryPresentationComposer composer)
    : IRequestHandler<ListAdminStoriesQuery, IReadOnlyList<AdminStorySnapshot>>
{
    /// <inheritdoc />
    public Task<IReadOnlyList<AdminStorySnapshot>> Handle(ListAdminStoriesQuery request, CancellationToken cancellationToken)
        => request.PendingReview
            ? composer.AdminListPendingReviewAsync(request.TenantId, cancellationToken)
            : composer.AdminListAsync(request.TenantId, request.ReviewStatus, cancellationToken);
}

/// <summary>گرید DB-native استوری Admin.</summary>
public sealed record QueryAdminStoryGridQuery(
    Guid TenantId,
    StoryReviewStatus? ReviewStatus,
    GridQueryRequest Request) : IRequest<GridPageResponse<AdminStorySnapshot>>;

/// <summary>Handler گرید Admin.</summary>
public sealed class QueryAdminStoryGridQueryHandler(StoryPresentationComposer composer)
    : IRequestHandler<QueryAdminStoryGridQuery, GridPageResponse<AdminStorySnapshot>>
{
    /// <inheritdoc />
    public Task<GridPageResponse<AdminStorySnapshot>> Handle(
        QueryAdminStoryGridQuery request, CancellationToken cancellationToken)
        => composer.QueryAdminGridAsync(request.TenantId, request.ReviewStatus, request.Request, cancellationToken);
}

/// <summary>جزئیات مدیریتی یک استوری.</summary>
public sealed record GetAdminStoryQuery(Guid TenantId, Guid StoryId) : IRequest<AdminStorySnapshot?>;

/// <summary>Handler جزئیات Admin.</summary>
public sealed class GetAdminStoryQueryHandler(StoryPresentationComposer composer)
    : IRequestHandler<GetAdminStoryQuery, AdminStorySnapshot?>
{
    /// <inheritdoc />
    public Task<AdminStorySnapshot?> Handle(GetAdminStoryQuery request, CancellationToken cancellationToken)
        => composer.AdminGetAsync(request.TenantId, request.StoryId, cancellationToken);
}
