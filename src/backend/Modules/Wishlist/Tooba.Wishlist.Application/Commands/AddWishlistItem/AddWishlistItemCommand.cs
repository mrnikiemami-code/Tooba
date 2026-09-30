using MediatR;
using Tooba.Wishlist.Application.Ports;

namespace Tooba.Wishlist.Application.Commands.AddWishlistItem;

/// <summary>افزودن idempotent محصول Published به Wishlist Actor.</summary>
public sealed record AddWishlistItemCommand(Guid ActorUserId, Guid ProductId) : IRequest<WishlistAddResult>;

/// <summary>Handler افزودن؛ فقط از دایرکتوری ماژول استفاده می‌کند.</summary>
public sealed class AddWishlistItemCommandHandler(IWishlistDirectory wishlist)
    : IRequestHandler<AddWishlistItemCommand, WishlistAddResult>
{
    /// <inheritdoc />
    public Task<WishlistAddResult> Handle(AddWishlistItemCommand request, CancellationToken cancellationToken)
        => wishlist.AddAsync(request.ActorUserId, request.ProductId, cancellationToken);
}
