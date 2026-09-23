using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Customer.Models;
using Tooba.Order.Application.Customer.Ports;

namespace Tooba.Order.Application.Customer.Queries.ListCustomerOrders;

/// <summary>فهرست سفارش‌های متعلق به Actor نشست.</summary>
public sealed record ListCustomerOrdersQuery(Guid ActorUserId)
    : IRequest<Result<IReadOnlyList<CustomerOrderListItem>>>;

/// <summary>Checkoutهای Actor را می‌خواند و به آیتم فهرست نگاشت می‌کند.</summary>
public sealed class ListCustomerOrdersHandler(
    ICustomerOrderCheckoutStore store,
    CustomerOrderComposer composer)
    : IRequestHandler<ListCustomerOrdersQuery, Result<IReadOnlyList<CustomerOrderListItem>>>
{
    public async Task<Result<IReadOnlyList<CustomerOrderListItem>>> Handle(
        ListCustomerOrdersQuery request,
        CancellationToken cancellationToken)
    {
        if (request.ActorUserId == Guid.Empty)
        {
            return Result.Failure<IReadOnlyList<CustomerOrderListItem>>(
                new SemanticError(CustomerOrderErrors.SessionRequired));
        }

        var groups = await store.ListByActorAsync(request.ActorUserId, take: 200, cancellationToken);
        var orders = new List<CustomerOrderListItem>(groups.Count);
        foreach (var group in groups)
        {
            orders.Add(await composer.MapListItemAsync(group, request.ActorUserId, cancellationToken));
        }

        return Result.Success<IReadOnlyList<CustomerOrderListItem>>(orders);
    }
}
