using Tooba.Cart.Application.Ports;
using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.BuildingBlocks.Security;
using Tooba.Cart.Application.Errors;
using Tooba.Cart.Contracts;
using Tooba.Cart.Application.Presentation;
using Tooba.Cart.Contracts;

namespace Tooba.Cart.Application.Commands.ChangeCartLineQuantity;

/// <summary>Changes a cart line quantity. Zero removes the line.</summary>
public sealed record ChangeCartLineQuantityCommand(
    Guid CartId,
    string? GuestSecret,
    int ExpectedVersion,
    Guid LineId,
    decimal Quantity) : IRequest<Result<CartPage>>;

internal sealed class ChangeCartLineQuantityHandler(
    ICartDirectory carts,
    CartPresentationComposer presentation,
    ICurrentAuthenticatedUser user) : IRequestHandler<ChangeCartLineQuantityCommand, Result<CartPage>>
{
    public Task<Result<CartPage>> Handle(ChangeCartLineQuantityCommand request, CancellationToken cancellationToken) =>
        CartExceptionMapper.TryAsync(async () =>
        {
            var snapshot = await carts.ChangeLineQuantityAsync(
                request.CartId,
                Access(request.GuestSecret),
                request.ExpectedVersion,
                request.LineId,
                request.Quantity,
                cancellationToken);
            return await presentation.PresentAsync(snapshot, guestSecret: null, cancellationToken);
        });

    private CartAccess Access(string? guestSecret) =>
        new(user.IsAuthenticated ? user.UserId : null, guestSecret);
}
