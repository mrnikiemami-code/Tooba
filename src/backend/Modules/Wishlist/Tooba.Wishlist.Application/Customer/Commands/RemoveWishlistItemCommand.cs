using MediatR;
using Tooba.Wishlist.Application.Ports;

namespace Tooba.Wishlist.Application.Customer.Commands;

/// <summary>حذف مرجع محصول از Wishlist Actor (idempotent).</summary>
public sealed record RemoveWishlistItemCommand(Guid ActorUserId, Guid ProductId) : IRequest;

/// <summary>Handler حذف؛ فقط از دایرکتوری ماژول استفاده می‌کند.</summary>
public sealed class RemoveWishlistItemCommandHandler(IWishlistDirectory wishlist)
    : IRequestHandler<RemoveWishlistItemCommand>
{
    /// <inheritdoc />
    public Task Handle(RemoveWishlistItemCommand request, CancellationToken cancellationToken)
        => wishlist.RemoveAsync(request.ActorUserId, request.ProductId, cancellationToken);
}
