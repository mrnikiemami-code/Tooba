using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.BuildingBlocks.Security;
using Tooba.Cart.Application.Composition;
using Tooba.Cart.Contracts.Errors;
using Tooba.Cart.Contracts;
using Tooba.Cart.Application.Presentation;

namespace Tooba.Cart.Application.Queries.GetCart;

/// <summary>Loads a cart by id after guest/authenticated access checks.</summary>
public sealed record GetCartQuery(Guid CartId, string? GuestSecret) : IRequest<Result<CartPage>>;

internal sealed class GetCartHandler(
    ICartQueryGateway cartQueries,
    CartPresentationComposer presentation,
    ICurrentAuthenticatedUser user) : IRequestHandler<GetCartQuery, Result<CartPage>>
{
    public Task<Result<CartPage>> Handle(GetCartQuery request, CancellationToken cancellationToken) =>
        CartOperation.ExecuteAsync(async () =>
        {
            var access = new CartAccess(user.IsAuthenticated ? user.UserId : null, request.GuestSecret);
            var snapshot = await cartQueries.GetCartAsync(request.CartId, access, cancellationToken);
            if (snapshot is null)
            {
                throw new SemanticException(new SemanticError(CartErrorCodes.Missing));
            }

            return await presentation.PresentAsync(snapshot, guestSecret: null, cancellationToken);
        });
}
