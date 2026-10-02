using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Story.Application.Stories.Composition;
using Tooba.Story.Application.Stories.Models;
using Tooba.Story.Application.Stories.Presentation;
using Tooba.Story.Contracts.Errors;

namespace Tooba.Story.Application.Stories.Queries.Seller;

/// <summary>فهرست استوری‌های فروشنده.</summary>
public sealed record ListSellerStoriesQuery(Guid TenantId, Guid SellerPartyId)
    : IRequest<Result<IReadOnlyList<AdminStorySnapshot>>>;

/// <summary>Handler فهرست فروشنده.</summary>
public sealed class ListSellerStoriesQueryHandler(StoryPresentationComposer composer)
    : IRequestHandler<ListSellerStoriesQuery, Result<IReadOnlyList<AdminStorySnapshot>>>
{
    /// <inheritdoc />
    public Task<Result<IReadOnlyList<AdminStorySnapshot>>> Handle(
        ListSellerStoriesQuery request, CancellationToken cancellationToken)
        => StoryOperation.ExecuteAsync(
            () => composer.SellerListAsync(request.TenantId, request.SellerPartyId, cancellationToken));
}

/// <summary>جزئیات استوری فروشنده.</summary>
public sealed record GetSellerStoryQuery(Guid TenantId, Guid SellerPartyId, Guid StoryId)
    : IRequest<Result<AdminStorySnapshot>>;

/// <summary>Handler جزئیات فروشنده.</summary>
public sealed class GetSellerStoryQueryHandler(StoryPresentationComposer composer)
    : IRequestHandler<GetSellerStoryQuery, Result<AdminStorySnapshot>>
{
    /// <inheritdoc />
    public async Task<Result<AdminStorySnapshot>> Handle(
        GetSellerStoryQuery request, CancellationToken cancellationToken)
    {
        var wrapped = await StoryOperation.ExecuteAsync(
            () => composer.SellerGetAsync(
                request.TenantId, request.SellerPartyId, request.StoryId, cancellationToken));
        if (wrapped.IsFailure)
            return Result.Failure<AdminStorySnapshot>(wrapped.Errors);
        return StoryOperation.NotFoundIfNull(wrapped.Value, StoryErrorCodes.Missing);
    }
}
