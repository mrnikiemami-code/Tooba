using Tooba.Cart.Application.Ports;
using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.BuildingBlocks.Security;
using Tooba.Cart.Application.Composition;
using Tooba.Cart.Contracts.Errors;
using Tooba.Cart.Contracts;
using Tooba.Cart.Application.Presentation;

namespace Tooba.Cart.Application.Carts.Queries;

/// <summary>Returns the active authenticated cart without creating a guest shadow cart.</summary>
public sealed record GetCurrentAuthenticatedCartQuery : IRequest<Result<CartPage>>;

internal sealed class GetCurrentAuthenticatedCartHandler(
    ICartDirectory carts,
    CartPresentationComposer presentation,
    ICurrentAuthenticatedUser user) : IRequestHandler<GetCurrentAuthenticatedCartQuery, Result<CartPage>>
{
    public Task<Result<CartPage>> Handle(GetCurrentAuthenticatedCartQuery request, CancellationToken cancellationToken) =>
        CartOperation.ExecuteAsync(async () =>
        {
            if (!user.IsAuthenticated || user.UserId is not Guid userId || userId == Guid.Empty)
            {
                throw new SemanticException(new SemanticError(CartErrorCodes.AuthenticationRequired));
            }

            var snapshot = await carts.FindActiveAuthenticatedAsync(userId, cancellationToken);
            if (snapshot is null)
            {
                throw new SemanticException(new SemanticError(CartErrorCodes.Missing));
            }

            return await presentation.PresentAsync(snapshot, guestSecret: null, cancellationToken);
        });
}
