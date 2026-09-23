using Tooba.Cart.Application.Ports;
using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Cart.Application.Errors;
using Tooba.Cart.Contracts;
using Tooba.Cart.Application.Presentation;

namespace Tooba.Cart.Application.Commands.CreateGuestCart;

/// <summary>Creates an anonymous guest cart and returns the one-time raw guest secret.</summary>
public sealed record CreateGuestCartCommand : IRequest<Result<CartPage>>;

internal sealed class CreateGuestCartHandler(
    ICartDirectory carts,
    ICartCommerceContextResolver commerceContext,
    CartPresentationComposer presentation) : IRequestHandler<CreateGuestCartCommand, Result<CartPage>>
{
    public Task<Result<CartPage>> Handle(CreateGuestCartCommand request, CancellationToken cancellationToken) =>
        CartExceptionMapper.TryAsync(async () =>
        {
            var context = commerceContext.Resolve();
            var created = await carts.CreateGuestAsync(
                context.Market,
                context.Currency,
                context.Channel,
                cancellationToken);
            return await presentation.PresentAsync(created.Cart, created.GuestSecret, cancellationToken);
        });
}
