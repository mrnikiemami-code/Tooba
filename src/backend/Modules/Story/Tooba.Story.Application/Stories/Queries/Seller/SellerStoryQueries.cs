using MediatR;
using Tooba.Story.Application.Stories.Models;
using Tooba.Story.Application.Stories.Presentation;

namespace Tooba.Story.Application.Stories.Queries.Seller;

/// <summary>فهرست استوری‌های فروشنده.</summary>
public sealed record ListSellerStoriesQuery(Guid TenantId, Guid SellerPartyId)
    : IRequest<IReadOnlyList<AdminStorySnapshot>>;

/// <summary>Handler فهرست فروشنده.</summary>
public sealed class ListSellerStoriesQueryHandler(StoryPresentationComposer composer)
    : IRequestHandler<ListSellerStoriesQuery, IReadOnlyList<AdminStorySnapshot>>
{
    /// <inheritdoc />
    public Task<IReadOnlyList<AdminStorySnapshot>> Handle(ListSellerStoriesQuery request, CancellationToken cancellationToken)
        => composer.SellerListAsync(request.TenantId, request.SellerPartyId, cancellationToken);
}

/// <summary>جزئیات استوری فروشنده.</summary>
public sealed record GetSellerStoryQuery(Guid TenantId, Guid SellerPartyId, Guid StoryId)
    : IRequest<AdminStorySnapshot?>;

/// <summary>Handler جزئیات فروشنده.</summary>
public sealed class GetSellerStoryQueryHandler(StoryPresentationComposer composer)
    : IRequestHandler<GetSellerStoryQuery, AdminStorySnapshot?>
{
    /// <inheritdoc />
    public Task<AdminStorySnapshot?> Handle(GetSellerStoryQuery request, CancellationToken cancellationToken)
        => composer.SellerGetAsync(request.TenantId, request.SellerPartyId, request.StoryId, cancellationToken);
}
