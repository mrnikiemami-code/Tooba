using MediatR;
using Tooba.BuildingBlocks.Grid;
using Tooba.BuildingBlocks.Results;
using Tooba.Story.Application.Composition;
using Tooba.Story.Application.Stories.Models;
using Tooba.Story.Application.Stories.Presentation;
using Tooba.Story.Contracts.Errors;

namespace Tooba.Story.Application.Stories.Queries.Admin;

/// <summary>فهرست مدیریتی استوری — ReviewStatus transport string parsed in Application.</summary>
public sealed record ListAdminStoriesQuery(Guid TenantId, string? ReviewStatus, bool PendingReview)
    : IRequest<Result<IReadOnlyList<AdminStorySnapshot>>>;

/// <summary>Handler فهرست مدیریتی.</summary>
public sealed class ListAdminStoriesQueryHandler(StoryPresentationComposer composer)
    : IRequestHandler<ListAdminStoriesQuery, Result<IReadOnlyList<AdminStorySnapshot>>>
{
    /// <inheritdoc />
    public Task<Result<IReadOnlyList<AdminStorySnapshot>>> Handle(
        ListAdminStoriesQuery request, CancellationToken cancellationToken)
        => StoryOperation.ExecuteAsync(async () =>
        {
            if (request.PendingReview)
                return await composer.AdminListPendingReviewAsync(request.TenantId, cancellationToken);

            var parsed = StoryFailureMapper.RequireReviewStatus(request.ReviewStatus);
            return await composer.AdminListAsync(request.TenantId, parsed, cancellationToken);
        });
}

/// <summary>گرید DB-native استوری Admin.</summary>
public sealed record QueryAdminStoryGridQuery(
    Guid TenantId,
    string? ReviewStatus,
    GridQueryRequest Request) : IRequest<Result<GridPageResponse<AdminStorySnapshot>>>;

/// <summary>Handler گرید Admin.</summary>
public sealed class QueryAdminStoryGridQueryHandler(StoryPresentationComposer composer)
    : IRequestHandler<QueryAdminStoryGridQuery, Result<GridPageResponse<AdminStorySnapshot>>>
{
    /// <inheritdoc />
    public Task<Result<GridPageResponse<AdminStorySnapshot>>> Handle(
        QueryAdminStoryGridQuery request, CancellationToken cancellationToken)
        => StoryOperation.ExecuteAsync(() =>
        {
            var parsed = StoryFailureMapper.RequireReviewStatus(request.ReviewStatus);
            return composer.QueryAdminGridAsync(request.TenantId, parsed, request.Request, cancellationToken);
        });
}

/// <summary>جزئیات مدیریتی یک استوری.</summary>
public sealed record GetAdminStoryQuery(Guid TenantId, Guid StoryId)
    : IRequest<Result<AdminStorySnapshot>>;

/// <summary>Handler جزئیات Admin.</summary>
public sealed class GetAdminStoryQueryHandler(StoryPresentationComposer composer)
    : IRequestHandler<GetAdminStoryQuery, Result<AdminStorySnapshot>>
{
    /// <inheritdoc />
    public async Task<Result<AdminStorySnapshot>> Handle(
        GetAdminStoryQuery request, CancellationToken cancellationToken)
    {
        var wrapped = await StoryOperation.ExecuteAsync(
            () => composer.AdminGetAsync(request.TenantId, request.StoryId, cancellationToken));
        if (wrapped.IsFailure)
            return Result.Failure<AdminStorySnapshot>(wrapped.Errors);
        return StoryOperation.NotFoundIfNull(wrapped.Value, StoryErrorCodes.Missing);
    }
}
