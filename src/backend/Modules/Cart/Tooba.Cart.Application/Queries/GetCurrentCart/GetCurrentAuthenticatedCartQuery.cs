using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.BuildingBlocks.Security;
using Tooba.Cart.Application.Errors;
using Tooba.Cart.Application.Models;
using Tooba.Cart.Application.Ports;
using Tooba.Cart.Application.Presentation;

namespace Tooba.Cart.Application.Queries.GetCurrentCart;

/// <summary>Returns the active authenticated cart without creating a guest shadow cart.</summary>
public sealed record GetCurrentAuthenticatedCartQuery : IRequest<Result<CartPage>>;

internal sealed class GetCurrentAuthenticatedCartHandler(
    ICartDirectory carts,
    CartPresentationComposer presentation,
    ICurrentAuthenticatedUser user) : IRequestHandler<GetCurrentAuthenticatedCartQuery, Result<CartPage>>
{
    public Task<Result<CartPage>> Handle(GetCurrentAuthenticatedCartQuery request, CancellationToken cancellationToken) =>
        CartExceptionMapper.TryAsync(async () =>
        {
            if (!user.IsAuthenticated || user.UserId is not Guid userId || userId == Guid.Empty)
            {
                throw new InvalidOperationException(CartErrorCodes.AuthenticationRequired);
            }

            var snapshot = await carts.FindActiveAuthenticatedAsync(userId, cancellationToken);
            if (snapshot is null)
            {
                throw new InvalidOperationException(CartErrorCodes.Missing);
            }

            return await presentation.PresentAsync(snapshot, guestSecret: null, cancellationToken);
        });
}
