using Tooba.Cart.Application.Ports;
using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.BuildingBlocks.Security;
using Tooba.Cart.Application.Errors;
using Tooba.Cart.Contracts;
using Tooba.Cart.Application.Presentation;

namespace Tooba.Cart.Application.Commands.MergeCartAfterLogin;

/// <summary>Merges a proven anonymous cart into the authenticated active cart after login.</summary>
public sealed record MergeCartAfterLoginCommand(
    Guid? AnonymousCartId,
    string? GuestSecret) : IRequest<Result<CartPage>>;

internal sealed class MergeCartAfterLoginHandler(
    ICartDirectory carts,
    CartPresentationComposer presentation,
    ICurrentAuthenticatedUser user) : IRequestHandler<MergeCartAfterLoginCommand, Result<CartPage>>
{
    public Task<Result<CartPage>> Handle(MergeCartAfterLoginCommand request, CancellationToken cancellationToken) =>
        CartExceptionMapper.TryAsync(async () =>
        {
            if (!user.IsAuthenticated || user.UserId is not Guid userId || userId == Guid.Empty)
            {
                throw new InvalidOperationException(CartErrorCodes.AuthenticationRequired);
            }

            var merged = await carts.MergeAnonymousAfterLoginAsync(
                userId,
                request.AnonymousCartId,
                request.GuestSecret,
                cancellationToken);
            return await presentation.PresentAsync(merged.Cart, guestSecret: null, cancellationToken);
        });
}
