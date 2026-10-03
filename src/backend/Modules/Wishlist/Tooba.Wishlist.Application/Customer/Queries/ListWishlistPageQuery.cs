using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Wishlist.Application.Composition;
using Tooba.Wishlist.Application.Models;

namespace Tooba.Wishlist.Application.Customer.Queries;

/// <summary>صفحهٔ خصوصی Wishlist Actor با کارت‌های زندهٔ Catalog.</summary>
public sealed record ListWishlistPageQuery(Guid ActorUserId) : IRequest<Result<WishlistPage>>;

/// <summary>Handler فهرست ترکیبی؛ از PresentationComposer ماژول استفاده می‌کند.</summary>
public sealed class ListWishlistPageQueryHandler(WishlistPresentationComposer composer)
    : IRequestHandler<ListWishlistPageQuery, Result<WishlistPage>>
{
    /// <inheritdoc />
    public Task<Result<WishlistPage>> Handle(ListWishlistPageQuery request, CancellationToken cancellationToken)
        => WishlistOperation.ExecuteAsync(() => composer.ListAsync(request.ActorUserId, cancellationToken));
}
