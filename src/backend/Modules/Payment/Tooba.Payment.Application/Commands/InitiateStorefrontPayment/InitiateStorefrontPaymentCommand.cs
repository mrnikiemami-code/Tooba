using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Payment.Application.Errors;
using Tooba.Payment.Application.Models;

namespace Tooba.Payment.Application.Commands.InitiateStorefrontPayment;

public sealed record InitiateStorefrontPaymentCommand(
    Guid CheckoutId,
    Guid CartId,
    string? GuestSecret,
    string IdempotencyKey,
    bool UseWallet,
    string? ProviderCode,
    Guid? AuthenticatedUserId) : IRequest<Result<StorefrontPaymentInitiationDto>>;

public sealed class InitiateStorefrontPaymentHandler(StorefrontPaymentOrchestrator orchestrator)
    : IRequestHandler<InitiateStorefrontPaymentCommand, Result<StorefrontPaymentInitiationDto>>
{
    public Task<Result<StorefrontPaymentInitiationDto>> Handle(
        InitiateStorefrontPaymentCommand request, CancellationToken cancellationToken) =>
        PaymentExceptionMapper.TryAsync(() => orchestrator.InitiateAsync(
            request.CheckoutId,
            request.CartId,
            request.GuestSecret,
            request.IdempotencyKey,
            request.UseWallet,
            request.ProviderCode,
            request.AuthenticatedUserId,
            cancellationToken));
}
