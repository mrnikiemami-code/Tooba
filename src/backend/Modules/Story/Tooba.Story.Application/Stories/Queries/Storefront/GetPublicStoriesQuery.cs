using MediatR;
using Tooba.Story.Application.Stories.Models;
using Tooba.Story.Application.Stories.Presentation;

namespace Tooba.Story.Application.Stories.Queries.Storefront;

/// <summary>استوری‌های قابل نمایش عمومی فروشگاه.</summary>
public sealed record GetPublicStoriesQuery(Guid TenantId, string? Locale, string? Market)
    : IRequest<IReadOnlyList<PublicStoryCard>>;

/// <summary>Handler فهرست عمومی.</summary>
public sealed class GetPublicStoriesQueryHandler(StoryPresentationComposer composer)
    : IRequestHandler<GetPublicStoriesQuery, IReadOnlyList<PublicStoryCard>>
{
    /// <inheritdoc />
    public Task<IReadOnlyList<PublicStoryCard>> Handle(GetPublicStoriesQuery request, CancellationToken cancellationToken)
        => composer.GetPublicStoriesAsync(request.TenantId, request.Locale, request.Market, cancellationToken);
}
