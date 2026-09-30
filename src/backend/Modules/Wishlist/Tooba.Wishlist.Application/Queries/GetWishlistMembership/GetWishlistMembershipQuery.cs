using MediatR;
using Tooba.Wishlist.Application.Models;
using Tooba.Wishlist.Application.Ports;

namespace Tooba.Wishlist.Application.Queries.GetWishlistMembership;

/// <summary>عضویت گروهی محصولات در Wishlist Actor.</summary>
public sealed record GetWishlistMembershipQuery(Guid ActorUserId, IReadOnlyList<Guid> ProductIds)
    : IRequest<WishlistMembershipResult>;

/// <summary>Handler عضویت گروهی؛ فقط از دایرکتوری ماژول استفاده می‌کند.</summary>
public sealed class GetWishlistMembershipQueryHandler(IWishlistDirectory wishlist)
    : IRequestHandler<GetWishlistMembershipQuery, WishlistMembershipResult>
{
    /// <inheritdoc />
    public async Task<WishlistMembershipResult> Handle(
        GetWishlistMembershipQuery request,
        CancellationToken cancellationToken)
    {
        var membership = await wishlist.GetMembershipAsync(
            request.ActorUserId,
            request.ProductIds,
            cancellationToken);
        return new WishlistMembershipResult(membership);
    }
}
