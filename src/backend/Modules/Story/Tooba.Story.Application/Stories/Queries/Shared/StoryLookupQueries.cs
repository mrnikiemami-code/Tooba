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

/// <summary>پرس‌وجوی صفحه‌بندی‌شدهٔ گزینه‌های محصول برای CTA استوری.</summary>
public sealed record ListStoryProductOptionsQuery(
    Guid? SellerPartyId,
    string? Search,
    int Page,
    int PageSize)
    : IRequest<Result<StoryProductOptionsPage>>;

/// <summary>Handler گزینه‌های محصول استوری.</summary>
public sealed class ListStoryProductOptionsQueryHandler(IStoryProductOptionsPort products)
    : IRequestHandler<ListStoryProductOptionsQuery, Result<StoryProductOptionsPage>>
{
    /// <inheritdoc />
    public Task<Result<StoryProductOptionsPage>> Handle(
        ListStoryProductOptionsQuery request,
        CancellationToken cancellationToken)
        => StoryOperation.ExecuteAsync(async () =>
        {
            if (request.SellerPartyId is Guid shop && shop != Guid.Empty)
            {
                return await products.SearchForSellerAsync(
                    shop,
                    request.Search,
                    request.Page,
                    request.PageSize,
                    cancellationToken);
            }

            return await products.SearchPublishedAsync(
                request.Search,
                request.Page,
                request.PageSize,
                cancellationToken);
        });
}
