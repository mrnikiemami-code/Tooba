using Tooba.Cart.Application.Ports;
using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Cart.Application.Errors;
using Tooba.Cart.Contracts;
using Tooba.Cart.Application.Presentation;
using Tooba.Offer.Contracts.Dtos;

namespace Tooba.Cart.Application.Commands.CreateGuestCart;

/// <summary>Creates an anonymous guest cart and returns the one-time raw guest secret.</summary>
public sealed record CreateGuestCartCommand : IRequest<Result<CartPage>>;

internal sealed class CreateGuestCartHandler(
    ICartDirectory carts,
    CartPresentationComposer presentation) : IRequestHandler<CreateGuestCartCommand, Result<CartPage>>
{
    public Task<Result<CartPage>> Handle(CreateGuestCartCommand request, CancellationToken cancellationToken) =>
        CartExceptionMapper.TryAsync(async () =>
        {
            var created = await carts.CreateGuestAsync("IR", "IRR", SalesChannel.Marketplace, cancellationToken);
            return await presentation.PresentAsync(created.Cart, created.GuestSecret, cancellationToken);
        });
}
