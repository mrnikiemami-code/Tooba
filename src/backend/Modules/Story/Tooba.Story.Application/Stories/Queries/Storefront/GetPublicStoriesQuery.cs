using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Story.Application.Stories.Composition;
using Tooba.Story.Application.Stories.Models;
using Tooba.Story.Application.Stories.Presentation;

namespace Tooba.Story.Application.Stories.Queries.Storefront;

/// <summary>استوری‌های قابل نمایش عمومی فروشگاه.</summary>
public sealed record GetPublicStoriesQuery(Guid TenantId, string? Locale, string? Market)
    : IRequest<Result<IReadOnlyList<PublicStoryCard>>>;

/// <summary>Handler فهرست عمومی.</summary>
public sealed class GetPublicStoriesQueryHandler(StoryPresentationComposer composer)
    : IRequestHandler<GetPublicStoriesQuery, Result<IReadOnlyList<PublicStoryCard>>>
{
    /// <inheritdoc />
    public Task<Result<IReadOnlyList<PublicStoryCard>>> Handle(
        GetPublicStoriesQuery request, CancellationToken cancellationToken)
        => StoryOperation.ExecuteAsync(
            () => composer.GetPublicStoriesAsync(
                request.TenantId, request.Locale, request.Market, cancellationToken));
}
