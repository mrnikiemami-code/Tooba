using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Wishlist.Application.Composition;
using Tooba.Wishlist.Application.Models;
using Tooba.Wishlist.Application.Ports;

namespace Tooba.Wishlist.Application.Customer.Queries;

/// <summary>عضویت گروهی محصولات در Wishlist Actor.</summary>
public sealed record GetWishlistMembershipQuery(Guid ActorUserId, IReadOnlyList<Guid> ProductIds)
    : IRequest<Result<WishlistMembershipResult>>;

/// <summary>Handler عضویت گروهی؛ فقط از دایرکتوری ماژول استفاده می‌کند.</summary>
public sealed class GetWishlistMembershipQueryHandler(IWishlistDirectory wishlist)
    : IRequestHandler<GetWishlistMembershipQuery, Result<WishlistMembershipResult>>
{
    /// <inheritdoc />
    public Task<Result<WishlistMembershipResult>> Handle(
        GetWishlistMembershipQuery request,
        CancellationToken cancellationToken) =>
        WishlistOperation.ExecuteAsync(async () =>
        {
            var membership = await wishlist.GetMembershipAsync(
                request.ActorUserId,
                request.ProductIds,
                cancellationToken);
            return new WishlistMembershipResult(membership);
        });
}
