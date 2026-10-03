using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Wishlist.Application.Composition;
using Tooba.Wishlist.Application.Ports;

namespace Tooba.Wishlist.Application.Customer.Commands;

/// <summary>حذف مرجع محصول از Wishlist Actor (idempotent).</summary>
public sealed record RemoveWishlistItemCommand(Guid ActorUserId, Guid ProductId) : IRequest<Result>;

/// <summary>Handler حذف؛ فقط از دایرکتوری ماژول استفاده می‌کند.</summary>
public sealed class RemoveWishlistItemCommandHandler(IWishlistDirectory wishlist)
    : IRequestHandler<RemoveWishlistItemCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(RemoveWishlistItemCommand request, CancellationToken cancellationToken)
        => WishlistOperation.ExecuteAsync(() => wishlist.RemoveAsync(request.ActorUserId, request.ProductId, cancellationToken));
}
