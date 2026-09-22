using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.BuildingBlocks.Security;
using Tooba.Cart.Application.Errors;
using Tooba.Cart.Application.Models;
using Tooba.Cart.Application.Ports;
using Tooba.Cart.Application.Presentation;
using Tooba.Cart.Contracts;

namespace Tooba.Cart.Application.Commands.AddCartLine;

/// <summary>Adds or increases an Offer line on a cart.</summary>
public sealed record AddCartLineCommand(
    Guid CartId,
    string? GuestSecret,
    int ExpectedVersion,
    Guid OfferId,
    decimal Quantity,
    Guid? MerchandisingCampaignId = null) : IRequest<Result<CartPage>>;

internal sealed class AddCartLineHandler(
    ICartDirectory carts,
    CartPresentationComposer presentation,
    ICurrentAuthenticatedUser user) : IRequestHandler<AddCartLineCommand, Result<CartPage>>
{
    public Task<Result<CartPage>> Handle(AddCartLineCommand request, CancellationToken cancellationToken) =>
        CartExceptionMapper.TryAsync(async () =>
        {
            var snapshot = await carts.AddOrIncreaseLineAsync(
                request.CartId,
                Access(request.GuestSecret),
                request.ExpectedVersion,
                request.OfferId,
                request.Quantity,
                cancellationToken,
                request.MerchandisingCampaignId);
            return await presentation.PresentAsync(snapshot, guestSecret: null, cancellationToken);
        });

    private CartAccess Access(string? guestSecret) =>
        new(user.IsAuthenticated ? user.UserId : null, guestSecret);
}
