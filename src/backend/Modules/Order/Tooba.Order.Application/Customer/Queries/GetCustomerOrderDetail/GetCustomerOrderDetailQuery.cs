using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Customer.Models;
using Tooba.Order.Application.Customer.Ports;

namespace Tooba.Order.Application.Customer.Queries.GetCustomerOrderDetail;

/// <summary>جزئیات یک Checkout متعلق به Actor.</summary>
public sealed record GetCustomerOrderDetailQuery(Guid ActorUserId, Guid CheckoutId)
    : IRequest<Result<CustomerOrderDetailPage>>;

/// <summary>مالکیت PlacedByUserId را enforce می‌کند و صفحه را ترکیب می‌کند.</summary>
public sealed class GetCustomerOrderDetailHandler(
    ICustomerOrderCheckoutStore store,
    CustomerOrderComposer composer)
    : IRequestHandler<GetCustomerOrderDetailQuery, Result<CustomerOrderDetailPage>>
{
    public async Task<Result<CustomerOrderDetailPage>> Handle(
        GetCustomerOrderDetailQuery request,
        CancellationToken cancellationToken)
    {
        if (request.ActorUserId == Guid.Empty)
        {
            return Result.Failure<CustomerOrderDetailPage>(
                new SemanticError(CustomerOrderErrors.SessionRequired));
        }

        var group = await store.GetOwnedAsync(request.ActorUserId, request.CheckoutId, cancellationToken);
        if (group is null)
        {
            return Result.Failure<CustomerOrderDetailPage>(
                new SemanticError(CustomerOrderErrors.Missing));
        }

        var page = await composer.ComposeDetailAsync(group, request.ActorUserId, cancellationToken);
        return Result.Success(page);
    }
}
