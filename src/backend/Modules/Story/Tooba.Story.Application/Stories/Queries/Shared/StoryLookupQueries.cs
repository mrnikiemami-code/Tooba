using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Story.Application.Composition;
using Tooba.Story.Application.Stories.Ports;

namespace Tooba.Story.Application.Stories.Queries.Shared;

/// <summary>پرس‌وجوی گزینه‌های زبان فعال برای ویرایشگر استوری.</summary>
public sealed record ListStoryLocaleOptionsQuery()
    : IRequest<Result<IReadOnlyList<StoryLocaleOption>>>;

/// <summary>Handler گزینه‌های زبان استوری.</summary>
public sealed class ListStoryLocaleOptionsQueryHandler(IStoryLocaleOptionsPort locales)
    : IRequestHandler<ListStoryLocaleOptionsQuery, Result<IReadOnlyList<StoryLocaleOption>>>
{
    /// <inheritdoc />
    public Task<Result<IReadOnlyList<StoryLocaleOption>>> Handle(
        ListStoryLocaleOptionsQuery request,
        CancellationToken cancellationToken)
        => StoryOperation.ExecuteAsync(() => locales.ListEnabledAsync(cancellationToken));
}

/// <summary>پرس‌وجوی گزینه‌های محصول برای CTA استوری (فروشگاه یا ویترین اصلی).</summary>
public sealed record ListStoryProductOptionsQuery(Guid? SellerPartyId)
    : IRequest<Result<IReadOnlyList<StoryProductOption>>>;

/// <summary>Handler گزینه‌های محصول استوری.</summary>
public sealed class ListStoryProductOptionsQueryHandler(IStoryProductOptionsPort products)
    : IRequestHandler<ListStoryProductOptionsQuery, Result<IReadOnlyList<StoryProductOption>>>
{
    /// <inheritdoc />
    public Task<Result<IReadOnlyList<StoryProductOption>>> Handle(
        ListStoryProductOptionsQuery request,
        CancellationToken cancellationToken)
        => StoryOperation.ExecuteAsync(async () =>
        {
            if (request.SellerPartyId is Guid shop && shop != Guid.Empty)
                return await products.ListForSellerAsync(shop, cancellationToken);
            return await products.ListPublishedAsync(cancellationToken);
        });
}
