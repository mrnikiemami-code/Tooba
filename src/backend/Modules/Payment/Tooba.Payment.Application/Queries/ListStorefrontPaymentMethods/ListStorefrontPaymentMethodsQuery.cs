using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Payment.Application.Models;

namespace Tooba.Payment.Application.Queries.ListStorefrontPaymentMethods;

public sealed record ListStorefrontPaymentMethodsQuery : IRequest<Result<StorefrontPaymentMethodsDto>>;

public sealed class ListStorefrontPaymentMethodsHandler(StorefrontPaymentOrchestrator orchestrator)
    : IRequestHandler<ListStorefrontPaymentMethodsQuery, Result<StorefrontPaymentMethodsDto>>
{
    public Task<Result<StorefrontPaymentMethodsDto>> Handle(
        ListStorefrontPaymentMethodsQuery request, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        return Task.FromResult(Result.Success(orchestrator.ListPaymentMethods()));
    }
}
