using MediatR;
using Tooba.Wishlist.Application.Models;
using Tooba.Wishlist.Application.Presentation;

namespace Tooba.Wishlist.Application.Queries.ListWishlistPage;

/// <summary>صفحهٔ خصوصی Wishlist Actor با کارت‌های زندهٔ Catalog.</summary>
public sealed record ListWishlistPageQuery(Guid ActorUserId) : IRequest<WishlistPage>;

/// <summary>Handler فهرست ترکیبی؛ از PresentationComposer ماژول استفاده می‌کند.</summary>
public sealed class ListWishlistPageQueryHandler(WishlistPresentationComposer composer)
    : IRequestHandler<ListWishlistPageQuery, WishlistPage>
{
    /// <inheritdoc />
    public Task<WishlistPage> Handle(ListWishlistPageQuery request, CancellationToken cancellationToken)
        => composer.ListAsync(request.ActorUserId, cancellationToken);
}
