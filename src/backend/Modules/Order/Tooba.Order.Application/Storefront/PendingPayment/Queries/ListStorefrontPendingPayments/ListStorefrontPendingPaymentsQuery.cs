using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Storefront.Models;
using Tooba.Order.Application.Storefront.Services;

namespace Tooba.Order.Application.Storefront.PendingPayment.Queries.ListStorefrontPendingPayments;

public sealed record ListStorefrontPendingPaymentsQuery(StorefrontPendingPaymentQueryRequest? Body)
    : IRequest<Result<StorefrontPendingPaymentPage>>;

public sealed class ListStorefrontPendingPaymentsHandler(StorefrontPendingPaymentService pending)
    : IRequestHandler<ListStorefrontPendingPaymentsQuery, Result<StorefrontPendingPaymentPage>>
{
    public Task<Result<StorefrontPendingPaymentPage>> Handle(ListStorefrontPendingPaymentsQuery request, CancellationToken cancellationToken)
        => StorefrontOrderResult.ExecuteAsync(() => pending.ListAsync(request.Body, cancellationToken));
}
