using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.BuildingBlocks.Security;
using Tooba.Cart.Application.Errors;
using Tooba.Cart.Application.Models;
using Tooba.Cart.Application.Ports;
using Tooba.Cart.Application.Presentation;
using Tooba.Cart.Contracts;

namespace Tooba.Cart.Application.Commands.RemoveCartLine;

/// <summary>Removes a cart line.</summary>
public sealed record RemoveCartLineCommand(
    Guid CartId,
    string? GuestSecret,
    int ExpectedVersion,
    Guid LineId) : IRequest<Result<CartPage>>;

internal sealed class RemoveCartLineHandler(
    ICartDirectory carts,
    CartPresentationComposer presentation,
    ICurrentAuthenticatedUser user) : IRequestHandler<RemoveCartLineCommand, Result<CartPage>>
{
    public Task<Result<CartPage>> Handle(RemoveCartLineCommand request, CancellationToken cancellationToken) =>
        CartExceptionMapper.TryAsync(async () =>
        {
            var snapshot = await carts.RemoveLineAsync(
                request.CartId,
                Access(request.GuestSecret),
                request.ExpectedVersion,
                request.LineId,
                cancellationToken);
            return await presentation.PresentAsync(snapshot, guestSecret: null, cancellationToken);
        });

    private CartAccess Access(string? guestSecret) =>
        new(user.IsAuthenticated ? user.UserId : null, guestSecret);
}
