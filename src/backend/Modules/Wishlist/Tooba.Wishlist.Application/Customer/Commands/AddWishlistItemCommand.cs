using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Wishlist.Application.Composition;
using Tooba.Wishlist.Application.Models;
using Tooba.Wishlist.Application.Ports;

namespace Tooba.Wishlist.Application.Customer.Commands;

/// <summary>افزودن idempotent محصول Published به Wishlist Actor.</summary>
public sealed record AddWishlistItemCommand(Guid ActorUserId, Guid ProductId) : IRequest<Result<WishlistAddResult>>;

/// <summary>Handler افزودن؛ فقط از دایرکتوری ماژول استفاده می‌کند.</summary>
public sealed class AddWishlistItemCommandHandler(IWishlistDirectory wishlist)
    : IRequestHandler<AddWishlistItemCommand, Result<WishlistAddResult>>
{
    /// <inheritdoc />
    public Task<Result<WishlistAddResult>> Handle(AddWishlistItemCommand request, CancellationToken cancellationToken)
        => WishlistOperation.ExecuteAsync(() => wishlist.AddAsync(request.ActorUserId, request.ProductId, cancellationToken));
}
